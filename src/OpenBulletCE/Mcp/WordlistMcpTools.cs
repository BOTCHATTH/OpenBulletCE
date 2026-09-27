using System.ComponentModel;
using System.Text.Json;
using OpenBulletCE.ViewModels;
using ModelContextProtocol.Server;
using RuriLib.Models;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for wordlists and cookie lists.
/// </summary>
[McpServerToolType]
public sealed class WordlistMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Lists all registered wordlists.</summary>
    [McpServerTool(Name = "list_wordlists"),
     Description("Lists all wordlists registered in OpenBullet CE with name, path, type, purpose and line count. Wordlists feed data lines (USER:PASS etc.) to jobs.")]
    public string ListWordlists()
    {
        McpUi.Run(() => OB.WordlistManager.RefreshList());
        var list = OB.WordlistManager.Wordlists.Select(w => new
        {
            name = w.Name,
            path = w.Path,
            type = w.Type,
            purpose = w.Purpose,
            total = w.Total,
            temporary = w.Temporary
        });
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    /// <summary>Gets one wordlist plus a preview of its first lines.</summary>
    [McpServerTool(Name = "get_wordlist"),
     Description("Gets a wordlist by name including a preview of its first lines. Use to confirm the data format before assigning it to a job or debug run.")]
    public string GetWordlist(
        [Description("Wordlist name")] string name,
        [Description("How many preview lines to return (default 5)")] int previewLines = 5)
    {
        var w = OB.WordlistManager.Wordlists.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (w == null) return $"Wordlist not found: {name}. Use list_wordlists.";

        var preview = new List<string>();
        try
        {
            if (File.Exists(w.Path))
                preview = File.ReadLines(w.Path).Take(previewLines).ToList();
        }
        catch (Exception ex) { preview.Add($"<error reading file: {ex.Message}>"); }

        return JsonSerializer.Serialize(new
        {
            w.Name, w.Path, w.Type, w.Purpose, w.Total, w.Temporary,
            preview
        }, JsonOpts);
    }

    /// <summary>
    /// Creates a new wordlist file with the given lines and registers it.
    /// </summary>
    [McpServerTool(Name = "create_wordlist"),
     Description("Creates a wordlist file on disk and registers it in OpenBullet CE. Lines should match a wordlist type from get_environment_info (e.g. 'user:pass' for Default). The type is auto-recognized from the first line.")]
    public string CreateWordlist(
        [Description("Wordlist name (also used as file name)")] string name,
        [Description("The lines to write, one per line")] string[] lines,
        [Description("Optional purpose tag")] string purpose = "")
    {
        try
        {
            var fileName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine("Wordlists", fileName + ".txt");
            Directory.CreateDirectory("Wordlists");
            File.WriteAllLines(path, lines);

            var wordlist = WordlistManagerViewModel.FileToWordlist(path);
            wordlist.Purpose = purpose;
            McpUi.Run(() => OB.WordlistManager.Add(wordlist));

            return $"Created wordlist '{wordlist.Name}' with {wordlist.Total} lines (type: {wordlist.Type})";
        }
        catch (Exception ex) { return $"Failed to create wordlist: {ex.Message}"; }
    }

    /// <summary>Deletes a wordlist registration (file is left on disk).</summary>
    [McpServerTool(Name = "delete_wordlist"),
     Description("Removes a wordlist from OpenBullet CE. The file on disk is NOT deleted.")]
    public string DeleteWordlist(
        [Description("Wordlist name")] string name)
    {
        var w = OB.WordlistManager.Wordlists.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (w == null) return $"Wordlist not found: {name}";
        McpUi.Run(() => OB.WordlistManager.Remove(w));
        return $"Removed wordlist '{name}' (file kept at {w.Path})";
    }

    /// <summary>Lists all cookie lists (folders of cookie files) — the Cookie Edition data source.</summary>
    [McpServerTool(Name = "list_cookie_lists"),
     Description("Lists all cookie lists registered in OpenBullet CE. Cookie lists are folders of cookie files used as the data source in Cookie Edition configs.")]
    public string ListCookieLists()
    {
        McpUi.Run(() => OB.CookieManager.RefreshList());
        var list = OB.CookieManager.Cookies.Select(c => new
        {
            name = c.Name,
            folder = c.PathOwnerFolder,
            cookieFiles = c.TotalCookiesFiles
        });
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    /// <summary>Gets a cookie list plus a preview of its cookie file names.</summary>
    [McpServerTool(Name = "get_cookie_list"),
     Description("Gets a cookie list by name including a preview of contained cookie files.")]
    public string GetCookieList(
        [Description("Cookie list name")] string name,
        [Description("How many preview entries to return (default 10)")] int previewEntries = 10)
    {
        var c = OB.CookieManager.Cookies.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (c == null) return $"Cookie list not found: {name}. Use list_cookie_lists.";

        return JsonSerializer.Serialize(new
        {
            c.Name,
            folder = c.PathOwnerFolder,
            totalFiles = c.TotalCookiesFiles,
            preview = c.PathAllCookiesFiles.Take(previewEntries).Select(Path.GetFileName)
        }, JsonOpts);
    }
}
