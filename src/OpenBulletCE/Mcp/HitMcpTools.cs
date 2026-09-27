using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RuriLib.Models;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for the hits database.
/// </summary>
[McpServerToolType]
public sealed class HitMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private static object HitShape(Hit h) => new
    {
        id = h.Id,
        data = h.Data,
        type = h.Type,
        proxy = h.Proxy,
        date = h.Date,
        config = h.ConfigName,
        wordlist = h.WordlistName,
        captured = h.CapturedString
    };

    /// <summary>Lists hits, optionally filtered by config, newest first.</summary>
    [McpServerTool(Name = "list_hits"),
     Description("Lists hits from the database, newest first. Filter by config name and/or hit type (SUCCESS, CUSTOM, ...). Limited to 'take' entries.")]
    public string ListHits(
        [Description("Optional config name filter")] string configName = "",
        [Description("Optional hit type filter e.g. SUCCESS")] string type = "",
        [Description("Max hits to return (default 50, max 500)")] int take = 50)
    {
        take = Math.Clamp(take, 1, 500);
        McpUi.Run(() => OB.HitsDB.RefreshList());

        var q = OB.HitsDB.Hits.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(configName))
            q = q.Where(h => h.ConfigName.Equals(configName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(h => h.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        var hits = q.OrderByDescending(h => h.Date).Take(take).Select(HitShape);
        return JsonSerializer.Serialize(new { total = OB.HitsDB.Total, hits }, JsonOpts);
    }

    /// <summary>Gets one hit by its GUID.</summary>
    [McpServerTool(Name = "get_hit"),
     Description("Gets a single hit by its id (GUID) including captured variables.")]
    public string GetHit(
        [Description("The hit GUID")] string id)
    {
        McpUi.Run(() => OB.HitsDB.RefreshList());
        var h = OB.HitsDB.Hits.FirstOrDefault(x => x.Id.ToString() == id);
        if (h == null) return $"Hit not found: {id}";
        return JsonSerializer.Serialize(HitShape(h), JsonOpts);
    }

    /// <summary>Searches hits by substring over data/captured/variables.</summary>
    [McpServerTool(Name = "search_hits"),
     Description("Searches hits where data or captured output contains the query substring. Filter by config. Newest first.")]
    public string SearchHits(
        [Description("Substring to search in data and captures")] string query,
        [Description("Optional config name filter")] string configName = "",
        [Description("Max hits to return (default 50, max 500)")] int take = 50)
    {
        take = Math.Clamp(take, 1, 500);
        McpUi.Run(() => OB.HitsDB.RefreshList());

        var q = OB.HitsDB.Hits.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(configName))
            q = q.Where(h => h.ConfigName.Equals(configName, StringComparison.OrdinalIgnoreCase));

        var hits = q.Where(h =>
                (h.Data?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (h.CapturedString?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(h => h.Date)
            .Take(take)
            .Select(HitShape);

        return JsonSerializer.Serialize(hits, JsonOpts);
    }

    /// <summary>Deletes hits matching filters.</summary>
    [McpServerTool(Name = "delete_hits"),
     Description("Deletes hits matching filters (config name and/or hit type). If no filters are given, deletes ALL hits — use carefully.")]
    public string DeleteHits(
        [Description("Optional config name filter")] string configName = "",
        [Description("Optional hit type filter")] string type = "")
    {
        McpUi.Run(() => OB.HitsDB.RefreshList());

        var q = OB.HitsDB.Hits.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(configName))
            q = q.Where(h => h.ConfigName.Equals(configName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(h => h.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        var toDelete = q.ToList();
        if (toDelete.Count == 0) return "No hits matched the filters.";
        McpUi.Run(() => OB.HitsDB.Remove(toDelete));
        return $"Deleted {toDelete.Count} hits.";
    }

    /// <summary>Exports hits to a file in a given format.</summary>
    [McpServerTool(Name = "export_hits"),
     Description("Exports hits to a text file. Format placeholders: <DATA>, <PROXY>, <TYPE>, <DATE>, plus captured variable names like <CAPTURE(username)>. Default format exports 'data | captured'.")]
    public string ExportHits(
        [Description("Output file path")] string outputPath,
        [Description("Format per hit; default '<DATA> | <CAPTURED>'")] string format = "<DATA> | <CAPTURED>",
        [Description("Optional config name filter")] string configName = "",
        [Description("Optional hit type filter")] string type = "")
    {
        McpUi.Run(() => OB.HitsDB.RefreshList());

        var q = OB.HitsDB.Hits.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(configName))
            q = q.Where(h => h.ConfigName.Equals(configName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(h => h.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        var hits = q.ToList();
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllLines(outputPath, hits.Select(h =>
                format
                    .Replace("<DATA>", h.Data)
                    .Replace("<PROXY>", h.Proxy)
                    .Replace("<TYPE>", h.Type)
                    .Replace("<DATE>", h.Date.ToString("yyyy-MM-dd HH:mm:ss"))
                    .Replace("<CAPTURED>", h.CapturedString)));

            return $"Exported {hits.Count} hits to {Path.GetFullPath(outputPath)}";
        }
        catch (Exception ex) { return $"Export failed: {ex.Message}"; }
    }
}
