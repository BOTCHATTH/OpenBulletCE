using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools exposing environment information: wordlist types, OS, plugins.
/// </summary>
[McpServerToolType]
public sealed class EnvironmentMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Returns environment details an agent needs to build configs.</summary>
    [McpServerTool(Name = "get_environment_info"),
     Description("Returns environment information: available wordlist types (for config allowedWordlists + debug data parsing), installed block plugins, custom keychains, OS and data folders.")]
    public string GetEnvironmentInfo()
    {
        var env = OB.Settings.Environment;
        return JsonSerializer.Serialize(new
        {
            version = OB.Version,
            edition = "Cookie Edition",
            scriptEngine = "LoliScript (.lce configs)",
            os = Environment.OSVersion.ToString(),
            wordlistTypes = env.WordlistTypes.Select(t => new
            {
                name = t.Name,
                regex = t.Regex,
                separator = t.Separator,
                slices = t.Slices
            }),
            customKeychains = env.CustomKeychains.Select(k => new { name = k.Name }),
            blockPlugins = OB.BlockPlugins.Select(p => new { name = p.Name, color = p.Color }),
            folders = new
            {
                configs = Path.GetFullPath(OB.configFolder),
                plugins = Path.GetFullPath(OB.pluginsFolder)
            },
            counts = new
            {
                configs = OB.ConfigManager.Total,
                wordlists = OB.WordlistManager.Total,
                cookieLists = OB.CookieManager.Total,
                proxies = OB.ProxyManager.Total,
                hits = OB.HitsDB.Total
            }
        }, JsonOpts);
    }
}
