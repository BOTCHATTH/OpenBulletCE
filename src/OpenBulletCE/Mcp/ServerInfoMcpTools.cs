using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools related to OpenBullet CE application state.
/// </summary>
[McpServerToolType]
public sealed class ServerInfoMcpTools
{
    /// <summary>
    /// Returns a small read-only summary of the current application state,
    /// including MCP server details (port, transport, tool count).
    /// </summary>
    [McpServerTool(Name = "get_server_info"),
     Description("Returns basic read-only information about the running OpenBullet CE application, including MCP server port, transport, and registered tool count.")]
    public string GetOpenBulletServerInfo()
    {
        var uptime = DateTime.UtcNow - McpGlobals.StartTime;
        var userDataFolder = Path.GetFullPath(".");
        var bind = McpServerHost.BindAddress;
        var port = McpServerHost.Port;

        var toolCount = 0;
        try
        {
            toolCount = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
                .SelectMany(t => t.GetMethods())
                .Count(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);
        }
        catch { /* best-effort count */ }

        return $$"""
               OC2 — OpenBullet Cookie 2 info
               Version: {{OB.Version}}
               StartedAtUtc: {{McpGlobals.StartTime:O}}
               Uptime: {{uptime:c}}
               UserDataFolder: {{userDataFolder}}

               MCP server info
               Status: {{McpServerHost.Status}}
               McpVersion: {{McpManifest.Version}}
               BindAddress: {{bind}}
               Port: {{port}}
               Endpoint: http://{{bind}}:{{port}}/mcp
               Transport: streamable-http
               ToolCount: {{toolCount}}
               """;
    }

    /// <summary>
    /// Returns the MCP feature version, full feature manifest, and changelog so
    /// agents can discover capabilities and what's new.
    /// </summary>
    [McpServerTool(Name = "get_mcp_version"),
     Description("Returns the MCP feature version, the full feature manifest (every tool grouped by capability), and the changelog of what's new in each version. CALL THIS at the start of a session to learn the server's capabilities — especially if your training data is older than the server.")]
    public string GetMcpVersion()
    {
        var features = McpManifest.Features
            .Select(f => new { group = f.Group, tools = f.Tools })
            .ToList();

        var changelog = McpManifest.Changelog
            .Select(c => new { version = c.Version, date = c.Date, changes = c.Changes })
            .ToList();

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            mcpVersion = McpManifest.Version,
            edition = "OC2 — OpenBullet Cookie 2 (LoliScript)",
            features,
            changelog
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
