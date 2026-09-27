using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using RuriLib;
using RuriLib.LS;
using RuriLib.LS;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for reading and surgically editing a config's LoliScript source.
/// </summary>
[McpServerToolType]
public sealed class ConfigScriptMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Returns the full LoliScript source of a config.</summary>
    [McpServerTool(Name = "get_loliscript"),
     Description("Returns the full LoliScript source of a config. Use get_script_map for a compact numbered view when you only need structure.")]
    public string GetLoliScript(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";
        return vm.Config.Script;
    }

    /// <summary>Replaces the entire script of a config.</summary>
    [McpServerTool(Name = "update_loliscript"),
     Description("Replaces the ENTIRE LoliScript source of a config. Prefer patch_script_region / block tools for surgical edits. Always call get_script_map first to know current structure.")]
    public string UpdateLoliScript(
        [Description("Config name")] string name,
        [Description("The complete new LoliScript source")] string script,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        var validation = ValidateLines(script);
        vm.Config.Script = script;
        McpConfigHelper.Save(vm);
        return $"Script replaced ({CountBlocks(script)} blocks). {validation}";
    }

    /// <summary>
    /// Returns a compact numbered map: every block line with its index, label
    /// and type. Non-block lines are summarized.
    /// </summary>
    [McpServerTool(Name = "get_script_map"),
     Description("Returns a compact numbered map of a config's script: each block line with its 0-based block index, type and label. Use the index with get_block/update_block/remove_block/move_block. CALL THIS FIRST before editing.")]
    public string GetScriptMap(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";

        var lines = SplitLines(vm.Config.Script);
        var spans = ConfigStackMcpTools.BlockLines(lines);
        var spanByStart = spans.ToDictionary(s => s.StartLine);
        var sb = new StringBuilder();
        int blockIdx = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            if (spanByStart.TryGetValue(i, out var span))
            {
                var t = raw.TrimStart('!');
                var type = BlockParser.GetBlockType(t);
                var label = ExtractLabel(t);
                var disabled = raw.StartsWith("!") ? " [DISABLED]" : "";
                var multi = span.EndLine > span.StartLine ? $" (lines {span.StartLine}-{span.EndLine})" : "";
                sb.AppendLine($"[{blockIdx}] line {i}{multi}: {type}{(label != null ? $" label=#{label}" : "")}{disabled}");
                blockIdx++;
                i = span.EndLine;
            }
            else if (!string.IsNullOrWhiteSpace(raw))
            {
                sb.AppendLine($"  line {i}: {Truncate(raw.Trim(), 80)}");
            }
        }
        sb.AppendLine($"Total: {blockIdx} blocks, {lines.Length} lines");
        return sb.ToString();
    }

    /// <summary>Returns a line range of the raw script.</summary>
    [McpServerTool(Name = "get_script_region"),
     Description("Returns raw script lines from startLine to endLine (0-based, inclusive). Line numbers come from get_script_map.")]
    public string GetScriptRegion(
        [Description("Config name")] string name,
        [Description("First line (0-based)")] int startLine,
        [Description("Last line (0-based, inclusive)")] int endLine,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";

        var lines = SplitLines(vm.Config.Script);
        if (lines.Length == 0) return "(empty script)";
        startLine = Math.Clamp(startLine, 0, lines.Length - 1);
        endLine = Math.Clamp(endLine, startLine, lines.Length - 1);

        var sb = new StringBuilder();
        for (int i = startLine; i <= endLine; i++)
            sb.AppendLine($"{i}: {lines[i]}");
        return sb.ToString();
    }

    /// <summary>Finds lines matching a substring or regex.</summary>
    [McpServerTool(Name = "find_in_script"),
     Description("Searches the config's script for a substring or regex. Returns matching line numbers with content.")]
    public string FindInScript(
        [Description("Config name")] string name,
        [Description("Text or regex to find")] string query,
        [Description("Treat query as regex")] bool isRegex = false,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";

        var lines = SplitLines(vm.Config.Script);
        var sb = new StringBuilder();
        System.Text.RegularExpressions.Regex? rx = null;
        if (isRegex)
        {
            try { rx = new System.Text.RegularExpressions.Regex(query); }
            catch (Exception ex) { return $"Invalid regex: {ex.Message}"; }
        }

        int count = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            bool match = rx != null
                ? rx.IsMatch(lines[i])
                : lines[i].Contains(query, StringComparison.OrdinalIgnoreCase);
            if (match)
            {
                sb.AppendLine($"{i}: {lines[i]}");
                if (++count >= 200) { sb.AppendLine("... (truncated at 200 matches)"); break; }
            }
        }
        return count == 0 ? "No matches." : sb.ToString();
    }

    /// <summary>
    /// Replaces a range of physical lines with new content — surgical edit.
    /// </summary>
    [McpServerTool(Name = "patch_script_region"),
     Description("Replaces raw script lines startLine..endLine (0-based inclusive) with new content. Safer than update_loliscript for edits. Get line numbers from get_script_map or find_in_script.")]
    public string PatchScriptRegion(
        [Description("Config name")] string name,
        [Description("First line to replace (0-based)")] int startLine,
        [Description("Last line to replace (0-based inclusive)")] int endLine,
        [Description("Replacement lines")] string newContent,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        var lines = SplitLines(vm.Config.Script).ToList();
        if (lines.Count == 0) return "Script is empty.";
        if (startLine < 0 || startLine >= lines.Count) return $"startLine {startLine} out of range (0..{lines.Count - 1})";
        endLine = Math.Clamp(endLine, startLine, lines.Count - 1);

        var replacement = newContent.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        lines.RemoveRange(startLine, endLine - startLine + 1);
        lines.InsertRange(startLine, replacement);

        var newScript = string.Join(Environment.NewLine, lines);
        var validation = ValidateLines(newScript);
        vm.Config.Script = newScript;
        McpConfigHelper.Save(vm);
        return $"Patched lines {startLine}-{endLine}. {validation}";
    }

    /// <summary>Validates the script: parses every block line.</summary>
    [McpServerTool(Name = "validate_script"),
     Description("Validates a config's script — every block line is parsed. Returns errors with line numbers. Run after editing.")]
    public string ValidateScript(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";
        return ValidateLines(vm.Config.Script);
    }

    internal static string[] SplitLines(string script)
        => (script ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

    /// <summary>
    /// Compresses a script like the LoliScript engine does: a block line absorbs
    /// following lines that start with space/tab or "! ". Returns logical lines.
    /// </summary>
    internal static List<string> CompressLines(IList<string> raw)
    {
        var lines = raw.ToList();
        var compressed = new List<string>();
        bool isScript = false;
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            if (trimmed.StartsWith("BEGIN SCRIPT")) isScript = true;
            else if (trimmed.StartsWith("END SCRIPT")) isScript = false;

            if (!isScript && BlockParser.IsBlock(line))
            {
                // absorb continuation lines
                while (i + 1 < lines.Count &&
                       (lines[i + 1].StartsWith(" ") || lines[i + 1].StartsWith("\t") ||
                        lines[i + 1].StartsWith("! ") || lines[i + 1].StartsWith("!\t")))
                {
                    var cont = lines[i + 1];
                    line += " " + (cont.StartsWith("!") ? cont.Substring(1).Trim() : cont.Trim());
                    i++;
                }
            }
            compressed.Add(line);
        }
        return compressed;
    }

    internal static int CountBlocks(string script)
        => CompressLines(SplitLines(script)).Count(l => BlockParser.IsBlock(l));

    internal static string ValidateLines(string script)
    {
        var lines = CompressLines(SplitLines(script));
        var errors = new List<string>();
        int blockCount = 0;
        int scriptDepth = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("BEGIN SCRIPT")) scriptDepth++;
            if (trimmed.StartsWith("END SCRIPT")) scriptDepth--;
            if (scriptDepth > 0) continue; // inside SCRIPT blocks lines are code, not blocks

            if (BlockParser.IsBlock(raw))
            {
                blockCount++;
                try { BlockParser.Parse(raw); }
                catch (Exception ex) { errors.Add($"line {i}: {ex.Message} | {Truncate(raw.Trim(), 80)}"); }
            }
        }

        if (scriptDepth != 0) errors.Add("Unbalanced BEGIN SCRIPT / END SCRIPT pairs");

        return errors.Count == 0
            ? $"OK — {blockCount} blocks, no parse errors."
            : $"{errors.Count} error(s):\n" + string.Join("\n", errors.Take(50));
    }

    internal static string? ExtractLabel(string line)
    {
        // Label syntax: #LABEL at the start of the line
        var t = line.TrimStart();
        if (!t.StartsWith("#")) return null;
        var end = t.IndexOf(' ');
        return end > 1 ? t.Substring(1, end - 1) : t.Substring(1);
    }

    internal static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max) + "...";
}
