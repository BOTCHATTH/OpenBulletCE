using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OpenBulletCE.Mcp;

/// <summary>
/// Knows how to write the OpenBullet CE MCP server entry into the config file
/// of every supported AI coding agent on Windows, macOS, and Linux.
/// Call <see cref="InstallForAgent"/> with the agent id and the live endpoint URL.
/// </summary>
public static class McpAgentInstaller
{
    /// <summary>
    /// Information about a supported agent: display name, id, config path resolver,
    /// and whether the config format is JSON or TOML.
    /// </summary>
    public sealed record AgentInfo(
        string Id,
        string DisplayName,
        string Description,
        Func<string> ConfigPathResolver,
        ConfigFormat Format,
        string ConfigKey = "mcpServers",
        string ServerKey = "oc2");

    public enum ConfigFormat { JsonMcpServers, JsonServers, TomlMcpServers }

    /// <summary>
    /// All supported agents, in display order.
    /// </summary>
    public static readonly List<AgentInfo> Agents =
    [
        new("claude-desktop", "Claude Desktop",
            "Anthropic Claude Desktop app",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Claude", "claude_desktop_config.json"),
            ConfigFormat.JsonMcpServers),

        new("claude-code", "Claude Code",
            "Anthropic Claude Code CLI",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude", "mcp.json"),
            ConfigFormat.JsonMcpServers),

        new("cursor", "Cursor",
            "Cursor IDE (global scope)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cursor", "mcp.json"),
            ConfigFormat.JsonMcpServers),

        new("windsurf", "Windsurf",
            "Codeium Windsurf IDE (global scope)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codeium", "windsurf", "mcp_config.json"),
            ConfigFormat.JsonMcpServers),

        new("copilot-cli", "GitHub Copilot CLI",
            "GitHub Copilot CLI (global scope)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".copilot", "mcp-config.json"),
            ConfigFormat.JsonMcpServers),

        new("vscode", "VS Code (Copilot)",
            "VS Code with Copilot — writes user-level mcp.json",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Code", "User", "mcp.json"),
            ConfigFormat.JsonServers,
            ConfigKey: "servers"),

        new("kiro", "Kiro IDE",
            "Kiro IDE (global scope)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".kiro", "settings", "mcp.json"),
            ConfigFormat.JsonMcpServers),

        new("gemini-cli", "Gemini CLI",
            "Google Gemini CLI (global scope)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini", "settings.json"),
            ConfigFormat.JsonMcpServers),

        new("codex", "Codex CLI",
            "OpenAI Codex CLI (global scope, TOML format)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codex", "config.toml"),
            ConfigFormat.TomlMcpServers),

        new("kimi", "Kimi",
            "Moonshot Kimi CLI (global scope)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".kimi", "mcp.json"),
            ConfigFormat.JsonMcpServers),

        new("zed", "Zed Editor",
            "Zed editor (global scope)",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Zed", "settings.json"),
            ConfigFormat.JsonMcpServers,
            ConfigKey: "mcp_servers"),

        new("devin", "Devin CLI",
            "Devin CLI — writes to every Devin MCP config that exists",
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "devin", "mcp_config.json"),
            ConfigFormat.JsonMcpServers,
            ServerKey: "oc2"),
    ];

    /// <summary>Every file Devin may read MCP servers from (it moved between versions).</summary>
    private static readonly string[] DevinConfigPaths =
    [
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "devin", "mcp_config.json"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "devin", "mcp_config.json"),
    ];

    /// <summary>
    /// Result of an install attempt.
    /// </summary>
    public sealed record InstallResult(
        string AgentId,
        string AgentName,
        bool Success,
        string ConfigPath,
        string? Error = null,
        bool CreatedDirectory = false,
        bool CreatedFile = false);

    /// <summary>
    /// Writes the OpenBullet CE MCP server entry into the agent's config file.
    /// Merges with existing config — does not overwrite other servers.
    /// </summary>
    public static async Task<InstallResult> InstallForAgent(
        string agentId,
        string endpointUrl,
        string? apiKey = null)
    {
        var agent = Agents.Find(a => a.Id == agentId);
        if (agent is null)
            return new InstallResult(agentId, agentId, false, "", $"Unknown agent '{agentId}'");

        // Devin moved its MCP config between versions — write every location
        // that exists so the server shows up regardless of which one is read.
        if (agentId == "devin")
        {
            var written = new List<string>();
            string? lastError = null;
            foreach (var path in DevinConfigPaths)
            {
                if (!File.Exists(path) && !Directory.Exists(Path.GetDirectoryName(path))) continue;
                var r = await WriteEntry(agent, path, endpointUrl, apiKey);
                if (r.Success) written.Add(path); else lastError = r.Error;
            }
            if (written.Count == 0)
                return new InstallResult(agent.Id, agent.DisplayName, false, "",
                    lastError ?? ("No Devin config found — looked in " + string.Join(", ", DevinConfigPaths)));
            return new InstallResult(agent.Id, agent.DisplayName, true,
                string.Join(" | ", written), $"installed into {written.Count} config(s)");
        }

        var configPath = agent.ConfigPathResolver();
        return await WriteEntry(agent, configPath, endpointUrl, apiKey);
    }

    /// <summary>Writes the OC2 server entry into a single agent config file.</summary>
    private static async Task<InstallResult> WriteEntry(
        AgentInfo agent, string configPath, string endpointUrl, string? apiKey)
    {
        var createdDir = false;
        var createdFile = false;

        try
        {
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                createdDir = true;
            }

            string content;
            if (File.Exists(configPath))
            {
                content = await File.ReadAllTextAsync(configPath);
            }
            else
            {
                content = agent.Format switch
                {
                    ConfigFormat.TomlMcpServers => "",
                    _ => "{}"
                };
                createdFile = true;
            }

            if (agent.Format == ConfigFormat.TomlMcpServers)
            {
                content = MergeToml(content, agent.ServerKey, endpointUrl, apiKey);
                // Write UTF-8 without BOM (Codex rejects BOM)
                await File.WriteAllTextAsync(configPath, content, new UTF8Encoding(false));
            }
            else
            {
                var json = MergeJson(content, agent.ConfigKey, agent.ServerKey, endpointUrl, apiKey);
                var opts = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null
                };
                var formatted = JsonSerializer.Serialize(json, opts);
                // Write UTF-8 without BOM (Windsurf and others reject BOM)
                await File.WriteAllTextAsync(configPath, formatted, new UTF8Encoding(false));
            }

            return new InstallResult(agent.Id, agent.DisplayName, true, configPath,
                null, createdDir, createdFile);
        }
        catch (Exception ex)
        {
            return new InstallResult(agent.Id, agent.DisplayName, false, configPath, ex.Message,
                createdDir, createdFile);
        }
    }

    /// <summary>
    /// Checks whether the agent's config file already contains an openbulletce entry.
    /// </summary>
    public static bool IsInstalled(string agentId)
    {
        var agent = Agents.Find(a => a.Id == agentId);
        if (agent is null) return false;

        if (agentId == "devin")
            return DevinConfigPaths.Any(p =>
                File.Exists(p) && File.ReadAllText(p).Contains("\"oc2\""));

        var configPath = agent.ConfigPathResolver();
        if (!File.Exists(configPath)) return false;

        try
        {
            var content = File.ReadAllText(configPath);
            if (agent.Format == ConfigFormat.TomlMcpServers)
                return content.Contains($"[mcp_servers.{agent.ServerKey}]") ||
                       content.Contains($"[mcp_servers.\"{agent.ServerKey}\"]") ||
                       content.Contains("[mcp_servers.openbulletce]");
            return content.Contains($"\"{agent.ServerKey}\"") || content.Contains("\"openbulletce\"");
        }
        catch { return false; }
    }

    /// <summary>
    /// Removes the openbulletce entry from the agent's config file.
    /// </summary>
    public static async Task<InstallResult> UninstallForAgent(string agentId)
    {
        var agent = Agents.Find(a => a.Id == agentId);
        if (agent is null)
            return new InstallResult(agentId, agentId, false, "", $"Unknown agent '{agentId}'");

        if (agentId == "devin")
        {
            var removed = new List<string>();
            foreach (var path in DevinConfigPaths.Where(File.Exists))
            {
                var r = await RemoveEntry(agent, path);
                if (r.Success) removed.Add(path);
            }
            return new InstallResult(agent.Id, agent.DisplayName, true,
                string.Join(" | ", removed),
                removed.Count == 0 ? "No Devin config had an OC2 entry" : $"removed from {removed.Count} config(s)");
        }

        var configPath = agent.ConfigPathResolver();
        if (!File.Exists(configPath))
            return new InstallResult(agent.Id, agent.DisplayName, true, configPath,
                "Config file does not exist — nothing to remove");
        return await RemoveEntry(agent, configPath);
    }

    /// <summary>Removes the OC2 entry from a single agent config file.</summary>
    private static async Task<InstallResult> RemoveEntry(AgentInfo agent, string configPath)
    {

        try
        {
            var content = await File.ReadAllTextAsync(configPath);

            if (agent.Format == ConfigFormat.TomlMcpServers)
            {
                content = RemoveTomlSection(content, agent.ServerKey);
                content = RemoveTomlSection(content, "openbulletce"); // legacy key
                await File.WriteAllTextAsync(configPath, content, new UTF8Encoding(false));
            }
            else
            {
                var json = JsonNode.Parse(content) ?? new JsonObject();
                if (json[agent.ConfigKey] is JsonObject servers)
                {
                    servers.Remove(agent.ServerKey);
                    servers.Remove("openbulletce"); // legacy key
                }
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var formatted = JsonSerializer.Serialize(json, opts);
                await File.WriteAllTextAsync(configPath, formatted, new UTF8Encoding(false));
            }

            return new InstallResult(agent.Id, agent.DisplayName, true, configPath,
                "OC2 entry removed");
        }
        catch (Exception ex)
        {
            return new InstallResult(agent.Id, agent.DisplayName, false, configPath, ex.Message);
        }
    }

    // ── JSON merge ──────────────────────────────────────────────────────

    private static JsonNode MergeJson(
        string existingContent, string configKey, string serverKey,
        string endpointUrl, string? apiKey)
    {
        JsonNode root;
        try
        {
            root = JsonNode.Parse(existingContent) ?? new JsonObject();
        }
        catch
        {
            // Corrupt JSON — start fresh
            root = new JsonObject();
        }

        if (root[configKey] is not JsonObject servers)
        {
            servers = new JsonObject();
            root[configKey] = servers;
        }

        // Build the server entry.
        // Most agents accept {"url": "..."} for streamable HTTP;
        // transport is required by Windsurf/Devin-style clients — harmless if ignored.
        var entry = new JsonObject { ["url"] = endpointUrl };
        if (configKey == "mcpServers")
            entry["transport"] = "streamable-http";

        if (!string.IsNullOrEmpty(apiKey))
            entry["headers"] = new JsonObject { ["Authorization"] = $"Bearer {apiKey}" };

        servers[serverKey] = entry;
        return root;
    }

    // ── TOML merge (Codex) ───────────────────────────────────────────────

    private static string MergeToml(
        string existing, string serverKey, string endpointUrl, string? apiKey)
    {
        // Remove any existing section for this key (and the legacy openbulletce key)
        existing = RemoveTomlSection(existing, serverKey);
        existing = RemoveTomlSection(existing, "openbulletce");

        var sb = new StringBuilder(existing);
        if (existing.Length > 0 && !existing.EndsWith('\n'))
            sb.AppendLine();

        sb.AppendLine();
        sb.AppendLine($"[mcp_servers.{serverKey}]");
        sb.AppendLine($"url = \"{endpointUrl}\"");

        if (!string.IsNullOrEmpty(apiKey))
            sb.AppendLine($"headers = {{ Authorization = \"Bearer {apiKey}\" }}");

        return sb.ToString();
    }

    private static string RemoveTomlSection(string content, string sectionName)
    {
        var lines = content.Split('\n');
        var sb = new StringBuilder();
        var skipping = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("[mcp_servers."))
            {
                // Check if this is our section
                var section = trimmed.Trim('[', ']', '"', '\'');
                if (section.EndsWith($".{sectionName}") ||
                    section == $"mcp_servers.{sectionName}")
                {
                    skipping = true;
                    continue;
                }
                skipping = false;
            }

            if (skipping)
            {
                // Skip lines until we hit another section header or blank line followed by content
                if (trimmed.StartsWith('['))
                {
                    skipping = false;
                    sb.AppendLine(line);
                }
                continue;
            }

            sb.AppendLine(line);
        }

        return sb.ToString().TrimEnd('\r', '\n') + Environment.NewLine;
    }
}
