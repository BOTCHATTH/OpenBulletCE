using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for installing this server into AI coding agents' configs.
/// </summary>
[McpServerToolType]
public sealed class McpSetupMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Lists supported agents and whether they're already installed.</summary>
    [McpServerTool(Name = "list_supported_agents"),
     Description("Lists all supported AI agents (Claude, Cursor, Windsurf, VS Code Copilot, Codex, Gemini, Kimi, Zed, Kiro...) with config path and whether the OpenBullet CE MCP entry is already installed.")]
    public string ListSupportedAgents()
    {
        var agents = McpAgentInstaller.Agents.Select(a => new
        {
            id = a.Id,
            name = a.DisplayName,
            description = a.Description,
            configPath = a.ConfigPathResolver(),
            installed = McpAgentInstaller.IsInstalled(a.Id)
        });
        return JsonSerializer.Serialize(agents, JsonOpts);
    }

    /// <summary>Writes the MCP endpoint into an agent's config file.</summary>
    [McpServerTool(Name = "install_mcp_in_agent"),
     Description("Writes the OpenBullet CE MCP server entry into an agent's config file (merges — does not overwrite other servers). Use list_supported_agents for ids. Agent picks it up on next restart.")]
    public async Task<string> InstallInAgent(
        [Description("Agent id from list_supported_agents")] string agentId)
    {
        if (McpServerHost.EndpointUrl == null)
            return "MCP server is not running — enable it in settings first.";

        var result = await McpAgentInstaller.InstallForAgent(agentId, McpServerHost.EndpointUrl,
            string.IsNullOrEmpty(McpServerHost.ApiKey) ? null : McpServerHost.ApiKey);

        return result.Success
            ? $"Installed for {result.AgentName} → {result.ConfigPath}" +
              (result.CreatedFile ? " (created file)" : "")
            : $"Failed for {result.AgentName}: {result.Error}";
    }

    /// <summary>Removes the MCP entry from an agent's config.</summary>
    [McpServerTool(Name = "uninstall_mcp_in_agent"),
     Description("Removes the OpenBullet CE MCP entry from an agent's config file.")]
    public async Task<string> UninstallFromAgent(
        [Description("Agent id")] string agentId)
    {
        var result = await McpAgentInstaller.UninstallForAgent(agentId);
        return result.Success
            ? $"Removed from {result.AgentName} ({result.ConfigPath})"
            : $"Failed for {result.AgentName}: {result.Error}";
    }
}
