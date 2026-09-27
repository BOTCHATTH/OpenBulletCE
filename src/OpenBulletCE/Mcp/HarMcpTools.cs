using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using RuriLib;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for analyzing HAR files and generating LoliScript configs from them.
/// </summary>
[McpServerToolType]
public sealed class HarMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Loads and summarizes a HAR file's requests.</summary>
    [McpServerTool(Name = "analyze_har_file"),
     Description("Analyzes a browser-recorded HAR file: lists each request (method, url, status), flags auth/login-looking calls, extracts interesting headers and bodies. Use to reconstruct a login flow into a config.")]
    public string AnalyzeHarFile(
        [Description("Path to the .har file")] string harPath,
        [Description("Only include requests to this host substring (optional)")] string hostFilter = "",
        [Description("Include request/response bodies (may be large)")] bool includeBodies = false)
    {
        var entries = LoadHar(harPath);
        if (entries == null) return $"Could not parse HAR: {harPath}";

        var filtered = entries.Where(e =>
            string.IsNullOrEmpty(hostFilter) ||
            e.Url.Contains(hostFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"HAR: {filtered.Count} requests{(hostFilter != "" ? $" (filtered by '{hostFilter}')" : "")}");
        for (int i = 0; i < filtered.Count; i++)
        {
            var e = filtered[i];
            var interesting = IsInteresting(e) ? " *" : "";
            sb.AppendLine($"[{i}] {e.Method} {e.Url} -> {e.Status}{interesting}");
            if (includeBodies)
            {
                if (!string.IsNullOrEmpty(e.RequestBody))
                    sb.AppendLine($"    req: {Truncate(e.RequestBody, 300)}");
                if (!string.IsNullOrEmpty(e.ResponseBody))
                    sb.AppendLine($"    res: {Truncate(e.ResponseBody, 300)}");
            }
            if (e.PostParams.Count > 0)
                sb.AppendLine($"    post params: {string.Join(", ", e.PostParams)}");
            var authHdrs = e.RequestHeaders.Where(h =>
                h.Key.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                h.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                h.Key.Contains("csrf", StringComparison.OrdinalIgnoreCase));
            foreach (var h in authHdrs)
                sb.AppendLine($"    hdr: {h.Key}: {Truncate(h.Value, 120)}");
        }
        sb.AppendLine("\n* = looks like an auth/login/token request");
        return sb.ToString();
    }

    /// <summary>Generates a LoliScript config draft from a HAR's login flow.</summary>
    [McpServerTool(Name = "generate_config_from_har"),
     Description("Generates a .lce LoliScript config draft from a HAR file: picks the most likely login request, substitutes <USER>/<PASS> for credentials, emits REQUEST + KEYCHECK. Review with get_script_map, then debug_config.")]
    public string GenerateConfigFromHar(
        [Description("Path to the .har file")] string harPath,
        [Description("Config name to create")] string configName,
        [Description("Index of the request to use (from analyze_har_file); -1 = auto-pick first auth-looking POST")] int requestIndex = -1,
        [Description("Category")] string category = "Default",
        [Description("Author")] string author = "MCP-HAR",
        [Description("Success string to keycheck (e.g. text appearing on login success)")] string successKey = "",
        [Description("Failure string to keycheck")] string failureKey = "")
    {
        var entries = LoadHar(harPath);
        if (entries == null) return $"Could not parse HAR: {harPath}";

        HarEntry? picked = null;
        if (requestIndex >= 0 && requestIndex < entries.Count) picked = entries[requestIndex];
        else picked = entries.FirstOrDefault(IsInteresting) ?? entries.FirstOrDefault(e => e.Method == "POST");

        if (picked == null) return "No suitable request found in HAR.";

        // Substitute credentials
        var body = picked.RequestBody;
        body = SubstituteCredentials(body, picked.PostParams);

        var sb = new StringBuilder();
        sb.AppendLine($"# Generated from HAR — {picked.Method} {picked.Url}");
        sb.AppendLine($"REQUEST {picked.Method} \"{picked.Url}\"" +
                      (body != null ? $" CONTENT \"{Escape(body)}\"" : ""));

        // Content type
        var contentType = picked.RequestHeaders
            .FirstOrDefault(h => h.Key.Equals("content-type", StringComparison.OrdinalIgnoreCase)).Value;
        if (!string.IsNullOrEmpty(contentType))
        {
            // rewrite last line appending CONTENTTYPE
            sb.Length -= Environment.NewLine.Length;
            sb.AppendLine($" CONTENTTYPE \"{Escape(contentType.Split(';')[0].Trim())}\"");
        }

        // Carry over important headers
        foreach (var h in picked.RequestHeaders)
        {
            var k = h.Key.ToLowerInvariant();
            if (k is "user-agent" or "accept" or "x-requested-with" || k.StartsWith("x-") || k.Contains("csrf") || k.Contains("token"))
                sb.AppendLine($"  HEADER \"{h.Key}: {Escape(h.Value)}\"");
        }

        sb.AppendLine();
        sb.AppendLine("KEYCHECK BanOnToCheck=TRUE");
        sb.AppendLine("  KEYCHAIN Success OR");
        sb.AppendLine(!string.IsNullOrEmpty(successKey)
            ? $"    KEY \"<SOURCE>\" Contains \"{Escape(successKey)}\""
            : $"    KEY \"<RESPONSECODE>\" EqualTo \"{(picked.Status == 200 ? "200" : "200")}\"");
        sb.AppendLine("  KEYCHAIN Failure OR");
        sb.AppendLine(!string.IsNullOrEmpty(failureKey)
            ? $"    KEY \"<SOURCE>\" Contains \"{Escape(failureKey)}\""
            : "    KEY \"<RESPONSECODE>\" EqualTo \"401\"");

        var script = sb.ToString();
        var result = new FastConfigMcpTools().WriteConfigScript(configName, script, category, author);

        return result + "\n\n--- Generated script ---\n" + script +
               "\n\nNext: set_config_settings (needsProxies, allowedWordlist1), " +
               "update_config_metadata, then debug_config with a real data line.";
    }

    // ---- internals ----

    private sealed record HarEntry(
        string Method, string Url, int Status,
        string RequestBody, string ResponseBody,
        List<KeyValuePair<string, string>> RequestHeaders,
        List<string> PostParams);

    private static bool IsInteresting(HarEntry e)
    {
        var u = e.Url.ToLowerInvariant();
        return e.Method == "POST" &&
               (u.Contains("login") || u.Contains("auth") || u.Contains("signin") ||
                u.Contains("token") || u.Contains("session") || u.Contains("account"));
    }

    private static string? SubstituteCredentials(string? body, List<string> postParams)
    {
        if (string.IsNullOrEmpty(body)) return body;
        var result = body;
        foreach (var p in postParams)
        {
            if (p.Contains("pass", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("pwd", StringComparison.OrdinalIgnoreCase))
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    $"({System.Text.RegularExpressions.Regex.Escape(p)}=)([^&\\s\"]+)", "$1<PASS>");
            else if (p.Contains("user", StringComparison.OrdinalIgnoreCase) ||
                     p.Contains("mail", StringComparison.OrdinalIgnoreCase) ||
                     p.Contains("login", StringComparison.OrdinalIgnoreCase))
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    $"({System.Text.RegularExpressions.Regex.Escape(p)}=)([^&\\s\"]+)", "$1<USER>");
        }
        return result;
    }

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max) + "...";

    private static List<HarEntry>? LoadHar(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var entries = doc.RootElement.GetProperty("log").GetProperty("entries");
            var list = new List<HarEntry>();

            foreach (var e in entries.EnumerateArray())
            {
                var req = e.GetProperty("request");
                var res = e.GetProperty("response");

                var headers = req.GetProperty("headers").EnumerateArray()
                    .Select(h => new KeyValuePair<string, string>(
                        h.GetProperty("name").GetString() ?? "",
                        h.GetProperty("value").GetString() ?? "")).ToList();

                string reqBody = "";
                var postParams = new List<string>();
                if (req.TryGetProperty("postData", out var pd))
                {
                    if (pd.TryGetProperty("text", out var t)) reqBody = t.GetString() ?? "";
                    if (pd.TryGetProperty("params", out var ps))
                        postParams = ps.EnumerateArray()
                            .Select(p => p.GetProperty("name").GetString() ?? "")
                            .Where(n => n != "").ToList();
                }

                string resBody = "";
                if (res.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("text", out var rt))
                {
                    var raw = rt.GetString() ?? "";
                    if (content.TryGetProperty("encoding", out var enc) &&
                        enc.GetString() == "base64")
                    {
                        try { resBody = Encoding.UTF8.GetString(Convert.FromBase64String(raw)); }
                        catch { resBody = raw; }
                    }
                    else resBody = raw;
                }

                list.Add(new HarEntry(
                    req.GetProperty("method").GetString() ?? "GET",
                    req.GetProperty("url").GetString() ?? "",
                    res.GetProperty("status").GetInt32(),
                    reqBody, resBody, headers, postParams));
            }
            return list;
        }
        catch { return null; }
    }
}
