using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using RuriLib;
using RuriLib.LS;
using RuriLib.Models;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for running configs in debug mode against a single data line.
/// </summary>
[McpServerToolType]
public sealed class ConfigDebugMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Runs a config's script on a test data line and reports the result.</summary>
    [McpServerTool(Name = "debug_config"),
     Description("Runs a config's LoliScript against a single test data line in a real bot (debug mode). Returns bot status, all variables, captures, and the full log. THE way to verify a config works — like clicking Start in the stacker debugger.")]
    public async Task<string> DebugConfig(
        [Description("Config name")] string name,
        [Description("Test data line, matching the config's wordlist type (e.g. 'user:pass' or a cookie path)")] string data,
        [Description("Optional proxy (host:port[:user:pass])")] string proxy = "",
        [Description("Proxy type: HTTP, HTTPS, SOCKS4, SOCKS4A, SOCKS5")] string proxyType = "HTTP",
        [Description("Wordlist type name for data slicing (default: first configured type)")] string wordlistType = "",
        [Description("Category")] string category = "",
        [Description("Abort after N seconds (default 60)")] int timeoutSeconds = 60)
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";
        if (string.IsNullOrEmpty(vm.Config.Script)) return "Config script is empty.";

        return await RunScript(vm.Config.Script, vm.Config.Settings, data, proxy, proxyType, wordlistType, timeoutSeconds);
    }

    /// <summary>Runs an ad-hoc script (no config needed) on a test data line.</summary>
    [McpServerTool(Name = "quick_test_config"),
     Description("Runs an ad-hoc LoliScript snippet on a test data line without creating a config. Use to prototype blocks before adding them. Provide minimal config settings like needsProxies.")]
    public async Task<string> QuickTestConfig(
        [Description("LoliScript source to run")] string script,
        [Description("Test data line")] string data,
        [Description("Optional proxy (host:port[:user:pass])")] string proxy = "",
        [Description("Proxy type")] string proxyType = "HTTP",
        [Description("Wordlist type for data slicing")] string wordlistType = "",
        [Description("Whether to actually use the given proxy")] bool useProxy = false,
        [Description("Abort after N seconds (default 60)")] int timeoutSeconds = 60)
    {
        if (string.IsNullOrWhiteSpace(script)) return "Script is empty.";
        var settings = new ConfigSettings { Name = "quick_test", NeedsProxies = useProxy };
        return await RunScript(script, settings, data, proxy, proxyType, wordlistType, timeoutSeconds);
    }

    private static async Task<string> RunScript(
        string script, ConfigSettings configSettings, string data, string proxyText,
        string proxyTypeName, string wordlistTypeName, int timeoutSeconds)
    {
        BotData? botData = null;
        string? fatal = null;
        string resolvedWType = "";
        bool dataValid = true;
        long elapsed = 0;

        await Task.Run(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                CProxy? proxy = null;
                if (!string.IsNullOrWhiteSpace(proxyText))
                {
                    var pType = Enum.TryParse<Extreme.Net.ProxyType>(proxyTypeName, true, out var pt)
                        ? pt : Extreme.Net.ProxyType.Http;
                    try { proxy = new CProxy().Parse(proxyText, pType); }
                    catch { proxy = new CProxy(proxyText, pType); }
                }

                var env = OB.Settings.Environment;
                WordlistType wType;
                if (!string.IsNullOrWhiteSpace(wordlistTypeName))
                    wType = env.GetWordlistType(wordlistTypeName);
                else if (configSettings.Type == RuriLib.Enums.ConfigType.CookieEdition)
                    wType = env.GetWordlistType("CE");
                else if (!string.IsNullOrWhiteSpace(configSettings.AllowedWordlist1))
                    wType = env.GetWordlistType(configSettings.AllowedWordlist1);
                else
                    wType = env.GetWordlistType(env.RecognizeWordlistType(data));
                wType ??= env.WordlistTypes.First();
                resolvedWType = wType.Name;
                var cData = new CData(data, wType);
                dataValid = cData.IsValid;

                botData = new BotData(
                    OB.Settings.RLSettings,
                    configSettings,
                    cData,
                    proxy,
                    proxy != null,
                    new Random());

                var ls = new LoliScript(script);
                ls.Reset();

                // TakeStep clears LogBuffer per block — accumulate like the debugger UI
                var fullLog = new List<LogEntry>();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                while (ls.CanProceed && !cts.IsCancellationRequested)
                {
                    try { ls.TakeStep(botData); }
                    catch (Exception ex)
                    {
                        botData.LogBuffer.Add(new LogEntry($"Error on {ls.CurrentLine}: {ex.Message}", Colors.Tomato));
                    }
                    fullLog.AddRange(botData.LogBuffer);
                }
                botData.LogBuffer.Clear();
                botData.LogBuffer.AddRange(fullLog);
            }
            catch (Exception ex) { fatal = ex.Message; }
            finally { sw.Stop(); elapsed = sw.ElapsedMilliseconds; }
        });

        if (fatal != null) return $"Failed to start debug run: {fatal}";
        if (botData == null) return "Debug run produced no data.";

        try { botData.Driver?.Quit(); } catch { }

        var variables = botData.Variables.All
            .Where(v => !v.Hidden)
            .Select(v => new { name = v.Name, type = v.Type.ToString(), capture = v.IsCapture, value = v.Value?.ToString() });

        var result = new
        {
            status = botData.StatusString,
            elapsedMs = elapsed,
            wordlistType = resolvedWType,
            dataValid,
            data = botData.Data.Data,
            proxy = botData.Proxy?.Proxy ?? "",
            address = SafeGet(() => botData.Address),
            responseCode = SafeGet(() => botData.ResponseCode),
            captures = botData.Variables.Captures.Select(v => new { v.Name, value = v.Value?.ToString() }),
            variables,
            log = botData.LogBuffer.Select(l => l.LogString)
        };

        return JsonSerializer.Serialize(result, JsonOpts);
    }

    private static string SafeGet(Func<string> f)
    {
        try { return f() ?? ""; } catch { return ""; }
    }
}
