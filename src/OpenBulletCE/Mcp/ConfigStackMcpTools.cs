using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using RuriLib;
using RuriLib.LS;
using RuriLib.LS;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools operating on a config's block stack. Blocks are addressed by
/// 0-based index (see get_script_map). Each LoliScript block is one line.
/// </summary>
[McpServerToolType]
public sealed class ConfigStackMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    // Per-config undo snapshots (single level)
    private static readonly Dictionary<string, string> _undo = new();

    /// <summary>Returns the full stack as indexed block lines.</summary>
    [McpServerTool(Name = "get_config_stack"),
     Description("Returns every block of the config's stack as an indexed list with raw LoliScript text. Index 0 is first. Use indices with the other stack tools.")]
    public string GetConfigStack(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";

        var blocks = BlockLines(vm.Config.Script);
        var sb = new StringBuilder();
        for (int i = 0; i < blocks.Count; i++)
            sb.AppendLine($"[{i}] {blocks[i].Text.Trim()}");
        if (blocks.Count == 0) sb.AppendLine("(empty stack)");
        return sb.ToString();
    }

    /// <summary>Compact overview: index, type, label, disabled.</summary>
    [McpServerTool(Name = "get_stack_overview"),
     Description("Compact overview of the block stack: index, block type, label, disabled flag. Lighter than get_config_stack.")]
    public string GetStackOverview(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";

        var blocks = BlockLines(vm.Config.Script);
        var items = blocks.Select((b, i) =>
        {
            var t = b.Text.TrimStart('!', ' ', '\t');
            return new
            {
                index = i,
                type = BlockParser.GetBlockType(t),
                label = ConfigScriptMcpTools.ExtractLabel(t),
                disabled = b.Text.TrimStart().StartsWith("!"),
                line = b.StartLine
            };
        });
        return JsonSerializer.Serialize(items, JsonOpts);
    }

    /// <summary>Gets one block's raw line by index.</summary>
    [McpServerTool(Name = "get_block"),
     Description("Gets one block's raw LoliScript text by 0-based block index.")]
    public string GetBlock(
        [Description("Config name")] string name,
        [Description("Block index")] int index,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";
        var blocks = BlockLines(vm.Config.Script);
        if (index < 0 || index >= blocks.Count) return $"Index {index} out of range (0..{blocks.Count - 1})";
        return blocks[index].Text;
    }

    /// <summary>Inserts a block line at the given index (end if omitted).</summary>
    [McpServerTool(Name = "add_block"),
     Description("Adds a block (or a whole multi-line construct) to the stack. blockLine is raw LoliScript, e.g. 'REQUEST GET \"https://x.com\"', a disabled block '!REQUEST ...', or a MULTI-LINE construct sent with newlines in ONE call: KEYCHECK+KEYCHAIN/KEY children, FUNCTION Translate + KEY/VALUE dict lines, BEGIN SCRIPT..END SCRIPT, IF/ELSE/ENDIF, WHILE/ENDWHILE. Use get_block_template for ready syntax. -1 index = append at end.")]
    public string AddBlock(
        [Description("Config name")] string name,
        [Description("Raw LoliScript block text (may be multi-line)")] string blockLine,
        [Description("Position to insert (-1 = end)")] int index = -1,
        [Description("Category")] string category = "")
    {
        return Mutate(name, category, lines =>
        {
            var blocks = BlockLines(lines);
            var clean = blockLine.TrimEnd('\r', '\n');
            var trimmed = clean.TrimStart('!', ' ', '\t');
            var insertLines = clean.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

            if (!IsBlockOrConstruct(trimmed))
                return $"Not a valid block line: {Truncate(trimmed, 80)}. Must start with a block keyword (REQUEST, PARSE, KEYCHECK, FUNCTION, UTILITY, COOKIECONTAINER, ...) or a script construct (BEGIN SCRIPT..END SCRIPT, IF/ELSE/ENDIF, WHILE/ENDWHILE, JUMP #label, SET, PRINT, DELETE) — use list_available_blocks / get_block_template.";

            int begins = insertLines.Count(l => l.Trim().StartsWith("BEGIN SCRIPT"));
            int ends = insertLines.Count(l => l.Trim().StartsWith("END SCRIPT"));
            if (begins != ends)
                return "Unbalanced BEGIN SCRIPT / END SCRIPT — send the whole script block in ONE add_block call.";

            try
            {
                foreach (var l in ConfigScriptMcpTools.CompressLines(insertLines))
                {
                    var t = l.TrimStart('!', ' ', '\t');
                    if (BlockParser.IsBlock(t)) BlockParser.Parse(t);
                }
            }
            catch (Exception ex) { return $"Block rejected — {ex.Message}. Use get_block_template for exact syntax."; }

            int pos;
            if (index < 0 || index >= blocks.Count) pos = lines.Count;
            else pos = blocks[index].StartLine;
            lines.InsertRange(pos, insertLines);
            return null;
        });
    }

    /// <summary>True for parser blocks AND non-block script constructs (BEGIN SCRIPT, flow control, commands). Only the FIRST line decides for multi-line input.</summary>
    private static bool IsBlockOrConstruct(string trimmed)
    {
        var firstLine = trimmed.Split('\n')[0].TrimEnd('\r').TrimStart();
        if (firstLine.StartsWith("##")) return true; // comment line
        if (BlockParser.IsBlock(firstLine)) return true;
        var first = firstLine.Split(' ', '\t')[0].ToUpperInvariant();
        if (first is "BEGIN" or "END" or "IF" or "ELSE" or "ENDIF" or "WHILE" or "ENDWHILE" or "JUMP")
            return true;
        if (CommandParser.IsCommand(firstLine)) return true; // SET, PRINT, DELETE, MOUSEACTION
        if (first.StartsWith("#") && firstLine.Length > first.Length + 1)
            return IsBlockOrConstruct(firstLine.Substring(first.Length + 1)); // #LABEL <block|construct>
        return false;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "...";

    /// <summary>Removes the block at index.</summary>
    [McpServerTool(Name = "remove_block"),
     Description("Removes the block at the given index.")]
    public string RemoveBlock(
        [Description("Config name")] string name,
        [Description("Block index")] int index,
        [Description("Category")] string category = "")
    {
        return Mutate(name, category, lines =>
        {
            var blocks = BlockLines(lines);
            if (index < 0 || index >= blocks.Count) return $"Index {index} out of range (0..{blocks.Count - 1})";
            lines.RemoveRange(blocks[index].StartLine, blocks[index].EndLine - blocks[index].StartLine + 1);
            return null;
        });
    }

    /// <summary>Moves a block to a new index.</summary>
    [McpServerTool(Name = "move_block"),
     Description("Moves the block at 'fromIndex' to 'toIndex' in the stack.")]
    public string MoveBlock(
        [Description("Config name")] string name,
        [Description("Current block index")] int fromIndex,
        [Description("Destination index")] int toIndex,
        [Description("Category")] string category = "")
    {
        return Mutate(name, category, lines =>
        {
            var blocks = BlockLines(lines);
            if (fromIndex < 0 || fromIndex >= blocks.Count) return $"fromIndex {fromIndex} out of range";
            toIndex = Math.Clamp(toIndex, 0, blocks.Count - 1);

            var span = blocks[fromIndex];
            var spanLines = lines.Skip(span.StartLine).Take(span.EndLine - span.StartLine + 1).ToList();
            lines.RemoveRange(span.StartLine, span.EndLine - span.StartLine + 1);
            var blocks2 = BlockLines(lines);
            int pos = toIndex >= blocks2.Count ? lines.Count : blocks2[toIndex].StartLine;
            lines.InsertRange(pos, spanLines);
            return null;
        });
    }

    /// <summary>Clones a block to a new position.</summary>
    [McpServerTool(Name = "clone_block"),
     Description("Duplicates the block at 'index' and inserts the copy at 'insertAt' (-1 = right after).")]
    public string CloneBlock(
        [Description("Config name")] string name,
        [Description("Block index to clone")] int index,
        [Description("Insert position (-1 = after source)")] int insertAt = -1,
        [Description("Category")] string category = "")
    {
        return Mutate(name, category, lines =>
        {
            var blocks = BlockLines(lines);
            if (index < 0 || index >= blocks.Count) return $"Index {index} out of range";
            var span = blocks[index];
            var spanLines = lines.Skip(span.StartLine).Take(span.EndLine - span.StartLine + 1).ToList();
            int pos;
            if (insertAt < 0) pos = span.EndLine + 1;
            else if (insertAt >= blocks.Count) pos = lines.Count;
            else pos = blocks[insertAt].StartLine;
            lines.InsertRange(pos, spanLines);
            return null;
        });
    }

    /// <summary>Replaces a block's line entirely.</summary>
    [McpServerTool(Name = "update_block"),
     Description("Replaces the block at 'index' with a new raw LoliScript line. Use get_block first to see the current text.")]
    public string UpdateBlock(
        [Description("Config name")] string name,
        [Description("Block index")] int index,
        [Description("New raw LoliScript line")] string newLine,
        [Description("Category")] string category = "")
    {
        return Mutate(name, category, lines =>
        {
            var blocks = BlockLines(lines);
            if (index < 0 || index >= blocks.Count) return $"Index {index} out of range";
            var clean = newLine.TrimEnd('\r', '\n');
            var trimmed = clean.TrimStart('!', ' ', '\t');
            if (!IsBlockOrConstruct(trimmed))
                return $"Not a valid block line: {Truncate(trimmed, 80)}";
            try
            {
                foreach (var l in ConfigScriptMcpTools.CompressLines(ConfigScriptMcpTools.SplitLines(clean).ToList()))
                {
                    var t = l.TrimStart('!', ' ', '\t');
                    if (BlockParser.IsBlock(t)) BlockParser.Parse(t);
                }
            }
            catch (Exception ex) { return $"Block line rejected — {ex.Message}. Use get_block_template for exact syntax."; }
            var span = blocks[index];
            lines.RemoveRange(span.StartLine, span.EndLine - span.StartLine + 1);
            lines.InsertRange(span.StartLine, clean.Split('\n').Select(l => l.TrimEnd('\r')));
            return null;
        });
    }

    /// <summary>Replaces the whole stack with given block lines.</summary>
    [McpServerTool(Name = "replace_stack"),
     Description("Replaces the entire stack with the given LoliScript lines. Non-block lines in the script (comments) are dropped — provide the full script if it has non-block lines.")]
    public string ReplaceStack(
        [Description("Config name")] string name,
        [Description("The new script lines")] string[] lines,
        [Description("Category")] string category = "")
    {
        return Mutate(name, category, script =>
        {
            script.Clear();
            script.AddRange(lines.Select(l => l.TrimEnd('\r')));
            return null;
        });
    }

    /// <summary>Reverts the last stack/script mutation done via MCP tools.</summary>
    [McpServerTool(Name = "undo_last_change"),
     Description("Undoes the last MCP-driven edit to this config (single level).")]
    public string UndoLastChange(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        var key = Key(name, category);
        if (!_undo.TryGetValue(key, out var previous))
            return "Nothing to undo.";

        vm.Config.Script = previous;
        McpConfigHelper.Save(vm);
        _undo.Remove(key);
        return $"Undone — restored previous script ({ConfigScriptMcpTools.CountBlocks(previous)} blocks).";
    }

    /// <summary>Validates the stack — parses every block.</summary>
    [McpServerTool(Name = "validate_stack"),
     Description("Validates the stack by parsing every block line. Returns per-line errors.")]
    public string ValidateStack(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";
        return ConfigScriptMcpTools.ValidateLines(vm.Config.Script);
    }

    /// <summary>No-op commit — writes are already saved on every mutation.</summary>
    [McpServerTool(Name = "commit_stack"),
     Description("Commits pending changes. Cookie Edition saves on every mutation, so this just re-saves and revalidates — safe to call at the end of an editing session.")]
    public string CommitStack(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";
        McpConfigHelper.Save(vm);
        return $"Saved '{name}'. {ConfigScriptMcpTools.ValidateLines(vm.Config.Script)}";
    }

    // ---- helpers ----

    /// <summary>A block's span of physical lines (multi-line blocks like KEYCHECK span several).</summary>
    internal record BlockSpan(int StartLine, int EndLine, string Text);

    internal static List<BlockSpan> BlockLines(IList<string> lines)
    {
        var result = new List<BlockSpan>();
        var scriptDepth = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            var t = lines[i].Trim();
            if (t.StartsWith("BEGIN SCRIPT")) { scriptDepth++; continue; }
            if (t.StartsWith("END SCRIPT")) { scriptDepth--; continue; }
            if (scriptDepth > 0) continue;

            if (BlockParser.IsBlock(lines[i]))
            {
                int end = i;
                // absorb continuation lines (space/tab or "! " prefix)
                while (end + 1 < lines.Count &&
                       (lines[end + 1].StartsWith(" ") || lines[end + 1].StartsWith("\t") ||
                        lines[end + 1].StartsWith("! ") || lines[end + 1].StartsWith("!\t")))
                    end++;
                result.Add(new BlockSpan(i, end, string.Join("\n", lines.Skip(i).Take(end - i + 1))));
                i = end;
            }
        }
        return result;
    }

    internal static List<BlockSpan> BlockLines(string script)
        => BlockLines(ConfigScriptMcpTools.SplitLines(script).ToList());

    private static string Key(string name, string category)
        => $"{category}/{name}".ToLowerInvariant();

    private string Mutate(string name, string category, Func<List<string>, string?> op)
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        var lines = ConfigScriptMcpTools.SplitLines(vm.Config.Script).ToList();
        var before = vm.Config.Script;

        var error = op(lines);
        if (error != null) return error;

        var newScript = string.Join(Environment.NewLine, lines);
        var validation = ConfigScriptMcpTools.ValidateLines(newScript);

        _undo[Key(name, category)] = before;
        vm.Config.Script = newScript;
        McpConfigHelper.Save(vm);
        return validation;
    }
}
