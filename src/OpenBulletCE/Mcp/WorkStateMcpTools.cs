using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for persisting agent work state across context compaction.
/// </summary>
[McpServerToolType]
public sealed class WorkStateMcpTools
{
    private static string StateDir => Path.Combine("McpState");

    /// <summary>Saves a named checkpoint so it survives context loss.</summary>
    [McpServerTool(Name = "save_checkpoint"),
     Description("Saves a named text checkpoint (e.g. current task state, decisions, next steps) that survives context compaction and agent restarts. Stored under McpState/.")]
    public string SaveCheckpoint(
        [Description("Checkpoint name, e.g. 'netflix-config-task'")] string name,
        [Description("The state text to save")] string content)
    {
        try
        {
            Directory.CreateDirectory(StateDir);
            var file = Path.Combine(StateDir, Sanitize(name) + ".txt");
            File.WriteAllText(file, $"# checkpoint: {name}\n# saved: {DateTime.Now:O}\n\n{content}");
            return $"Checkpoint saved ({file})";
        }
        catch (Exception ex) { return $"Failed: {ex.Message}"; }
    }

    /// <summary>Reads back a checkpoint.</summary>
    [McpServerTool(Name = "get_checkpoint"),
     Description("Reads a saved checkpoint by name. Omit name to list all checkpoints.")]
    public string GetCheckpoint(
        [Description("Checkpoint name (empty = list all)")] string name = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (!Directory.Exists(StateDir)) return "No checkpoints.";
                var files = Directory.GetFiles(StateDir, "*.txt").Select(Path.GetFileNameWithoutExtension);
                return files.Any()
                    ? "Checkpoints: " + string.Join(", ", files)
                    : "No checkpoints.";
            }

            var file = Path.Combine(StateDir, Sanitize(name) + ".txt");
            return File.Exists(file) ? File.ReadAllText(file) : $"Checkpoint not found: {name}";
        }
        catch (Exception ex) { return $"Failed: {ex.Message}"; }
    }

    /// <summary>Deletes a checkpoint.</summary>
    [McpServerTool(Name = "clear_checkpoint"),
     Description("Deletes a named checkpoint (or all with name='*').")]
    public string ClearCheckpoint(
        [Description("Checkpoint name, or * for all")] string name)
    {
        try
        {
            if (name == "*")
            {
                if (Directory.Exists(StateDir))
                    foreach (var f in Directory.GetFiles(StateDir, "*.txt")) File.Delete(f);
                return "All checkpoints cleared.";
            }

            var file = Path.Combine(StateDir, Sanitize(name) + ".txt");
            if (!File.Exists(file)) return $"Checkpoint not found: {name}";
            File.Delete(file);
            return $"Checkpoint '{name}' cleared.";
        }
        catch (Exception ex) { return $"Failed: {ex.Message}"; }
    }

    private static string Sanitize(string name)
        => string.Concat(name.Split(Path.GetInvalidFileNameChars()));
}
