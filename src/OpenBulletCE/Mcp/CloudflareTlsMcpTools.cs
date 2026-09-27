using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for Cloudflare / TLS-fingerprinting guidance, plus an analyzer that
/// inspects a response (or URL) for CF challenge markers.
/// </summary>
[McpServerToolType]
public sealed class CloudflareTlsMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Full bypass playbook.</summary>
    [McpServerTool(Name = "get_cloudflare_guidance"),
     Description("Returns the Cloudflare/TLS bypass playbook for Cookie Edition: when to use CURLREQUEST (curl-impersonate browser profiles), BYPASSCF (Selenium IUAM solver), and proxy strategy. READ THIS before fighting a 403/1020/cf-chl block.")]
    public string GetCloudflareGuidance()
    {
        return """
CLOUDFLARE / TLS BYPASS — COOKIE EDITION
========================================

LEVELS OF PROTECTION (weakest → strongest)
 1. Plain CF (managed challenge OFF, just WAF): works with normal REQUEST.
 2. TLS fingerprint check (JA3/JA4): 403 with cf-mitigated header even for
    correct requests. FIX: use the CURLREQUEST block (CurlTls plugin).
 3. JS challenge / IUAM ("Just a moment", cf-chl): needs a real browser.
    FIX: BYPASSCF block (Selenium) once to get cf_clearance, then REQUESTs.
 4. Turnstile / interactive captcha on login form: solve via SOLVECAPTCHA,
    or do the whole flow in Selenium.

TLS IMPERSONATION — CURLREQUEST block (built-in CurlTls plugin)
  CURLREQUEST "url" BROWSER "chrome120" [METHOD "POST"] [CONTENT "body"]
              [CTYPE "type"] [HEADER "k: v"] -> VAR "SOURCE"
 Profiles: chrome99..chrome146, safari15_3..safari18_0, firefox*, edge*, tor*.
 Matches real-browser JA3 + HTTP/2 fingerprint. Use instead of REQUEST when
 the target rejects non-browser TLS clients.
 Tip: try recent profiles first (chrome131+). Keep the same profile for the
 whole session — switching fingerprints mid-session looks suspicious.

IUAM CHALLENGE — BYPASSCF block
  BYPASSCF "url" [UA "useragent"] [SECPROTO Tls12]
 Runs headless Chrome via Selenium, waits for the challenge to clear, then
 harvests cf_clearance (+__cfduid) into the bot's cookie jar. Subsequent
 REQUEST calls reuse it (same UA!). Set config forceHeadless=true.

PROXY STRATEGY
 - cf_clearance is bound to the SOLVING IP — the job must keep the same proxy.
   Set config: onlySocks=false fine, but avoid proxy rotation mid-account-flow.
 - Datacenter IPs get harder challenges; residential/ISP proxies pass more.
 - Ban loop: if you see 1020/403 loops, raise banLoopEvasionOverride.

DIAGNOSIS
 Run debug_config and check the log + <RESPONSECODE>:
  403 + server: cloudflare + "cf-mitigated: challenge" → JS challenge → BYPASSCF.
  403 instantly (no JS) → TLS fingerprint → CURLREQUEST.
  1020 → WAF rule (IP/geo/ASN) → better proxies.
  429 → rate limit → slower CPM / more proxies.
 Use analyze_cloudflare_protection on a URL to auto-classify.
""";
    }

    /// <summary>Probes a URL and classifies the Cloudflare protection.</summary>
    [McpServerTool(Name = "analyze_cloudflare_protection"),
     Description("Sends a single GET to the URL and analyzes headers/body for Cloudflare markers (cf-mitigated, challenge tokens, JA3 rejection). Returns the protection level and a recommended bypass approach.")]
    public async Task<string> AnalyzeCloudflareProtection(
        [Description("The URL to probe")] string url,
        [Description("Optional proxy host:port")] string proxy = "")
    {
        try
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true };
            if (!string.IsNullOrWhiteSpace(proxy))
                handler.Proxy = new System.Net.WebProxy(proxy);

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

            var resp = await client.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            var headers = resp.Headers.Concat(resp.Content.Headers)
                .ToDictionary(h => h.Key.ToLowerInvariant(), h => string.Join(", ", h.Value));

            var server = headers.GetValueOrDefault("server", "");
            var cfMitigated = headers.GetValueOrDefault("cf-mitigated", "");
            var cfRay = headers.GetValueOrDefault("cf-ray", "");
            var isCF = server.Contains("cloudflare", StringComparison.OrdinalIgnoreCase) || cfRay != "";

            var markers = new List<string>();
            if (!isCF) markers.Add("no Cloudflare detected");
            if (cfMitigated.Contains("challenge")) markers.Add("cf-mitigated: challenge (IUAM JS challenge active)");
            if (body.Contains("cf-chl") || body.Contains("challenge-platform")) markers.Add("challenge platform in body (JS challenge)");
            if (body.Contains("turnstile")) markers.Add("Turnstile widget detected");
            if ((int)resp.StatusCode == 403 && isCF && markers.Count == 0) markers.Add("403 from CF without challenge markers — likely TLS fingerprint or WAF block");
            if ((int)resp.StatusCode == 1020) markers.Add("1020 Access Denied (WAF firewall rule)");

            var recommendation = !isCF ? "No Cloudflare — plain REQUEST blocks are fine."
                : markers.Any(m => m.Contains("challenge")) ? "JS challenge active — use BYPASSCF (Selenium) to harvest cf_clearance, then REQUEST."
                : (int)resp.StatusCode == 403 ? "Likely TLS fingerprinting — use CURLREQUEST with BROWSER \"chrome131\"."
                : (int)resp.StatusCode == 1020 ? "WAF block — rotate to better proxies (residential)."
                : "CF present but not challenging — plain REQUEST likely works; keep CURLREQUEST as fallback.";

            return JsonSerializer.Serialize(new
            {
                url,
                status = (int)resp.StatusCode,
                cloudflare = isCF,
                server,
                cfRay,
                cfMitigated,
                markers,
                recommendation
            }, JsonOpts);
        }
        catch (Exception ex) { return $"Probe failed: {ex.Message}"; }
    }

    /// <summary>Returns the correct CURLREQUEST line for a request.</summary>
    [McpServerTool(Name = "enable_curl_impersonation"),
     Description("Generates the correct CURLREQUEST (CurlTls plugin) LoliScript line for a request you describe — method, url, body, content type, headers. Equivalent to switching a REQUEST to TLS impersonation.")]
    public string EnableCurlImpersonation(
        [Description("HTTP method (GET/POST/...)")] string method,
        [Description("Request URL")] string url,
        [Description("Browser profile (chrome120..chrome146, safari*, firefox*, edge*)")] string browserProfile = "chrome131",
        [Description("Optional request body")] string body = "",
        [Description("Optional content type")] string contentType = "",
        [Description("Optional headers, one 'k: v' per line")] string headers = "",
        [Description("Output variable (default SOURCE)")] string outputVariable = "SOURCE")
    {
        var sb = new StringBuilder();
        sb.Append($"CURLREQUEST \"{url}\" BROWSER \"{browserProfile}\" METHOD \"{method.ToUpperInvariant()}\"");
        if (!string.IsNullOrEmpty(body)) sb.Append($" CONTENT \"{body}\"");
        if (!string.IsNullOrEmpty(contentType)) sb.Append($" CTYPE \"{contentType}\"");
        foreach (var h in headers.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            sb.Append($" HEADER \"{h.Trim()}\"");
        sb.Append($" -> VAR \"{outputVariable}\"");
        return sb.ToString();
    }
}
