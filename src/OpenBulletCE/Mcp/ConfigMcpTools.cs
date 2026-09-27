using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RuriLib;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBulletCE.Mcp;

/// <summary>Helpers shared by the config-related MCP tools.</summary>
internal static class McpConfigHelper
{
    /// <summary>Finds a ConfigViewModel by file name (optionally category).</summary>
    public static ConfigViewModel? Find(string name, string category = "")
        => McpUi.Run(() =>
        {
            OB.ConfigManager.Rescan();
            return OB.ConfigManager.Configs.FirstOrDefault(c =>
                c.FileName.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(category) || c.Category.Equals(category, StringComparison.OrdinalIgnoreCase)));
        });

    /// <summary>File path on disk for a config VM.</summary>
    public static string GetPath(ConfigViewModel vm)
        => string.IsNullOrEmpty(vm.Category) || vm.Category == "Default"
            ? Path.Combine(OB.configFolder, vm.FileName + ".lce")
            : Path.Combine(OB.configFolder, vm.Category, vm.FileName + ".lce");

    /// <summary>Persists a config to disk (and to the open config if it is loaded).</summary>
    public static void Save(ConfigViewModel vm)
    {
        vm.Config.Settings.LastModified = DateTime.Now;
        IOManager.SaveConfig(vm.Config, GetPath(vm));
        McpUi.Run(() =>
        {
            if (OB.ConfigManager.CurrentConfig?.FileName == vm.FileName &&
                OB.ConfigManager.CurrentConfig?.Category == vm.Category)
            {
                OB.ConfigManager.CurrentConfig.Config = vm.Config;
            }
        });
    }

    /// <summary>Loads a fresh copy of a config straight from disk.</summary>
    public static ConfigViewModel? FindFresh(string name, string category = "")
    {
        var vm = Find(name, category);
        if (vm == null) return null;
        var path = GetPath(vm);
        if (!File.Exists(path)) return vm;
        try { return new ConfigViewModel(vm.FileName, vm.Category, IOManager.LoadConfig(path), vm.Remote); }
        catch { return vm; }
    }

    /// <summary>Creates a config on disk + registers it. Returns a status string.</summary>
    public static string Create(string name, string category, string author, string script)
    {
        if (Find(name, category) != null)
            return $"A config named '{name}' already exists in category '{category}'.";

        var settings = new ConfigSettings
        {
            Name = name,
            Author = author,
            Type = RuriLib.Enums.ConfigType.CookieEdition
        };

        var vm = new ConfigViewModel(name, category, new Config(settings, script ?? ""));
        McpUi.Run(() => OB.ConfigManager.Add(vm));
        var validation = string.IsNullOrWhiteSpace(script)
            ? ""
            : " Script check: " + ConfigScriptMcpTools.ValidateLines(script);
        return $"Created config '{name}' ({GetPath(vm)}).{validation}";
    }
}

/// <summary>
/// MCP tools for config CRUD, metadata, settings and readme.
/// </summary>
[McpServerToolType]
public sealed class ConfigMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Lists all configs (.lce files) on disk.</summary>
    [McpServerTool(Name = "list_configs"),
     Description("Lists all OpenBullet CE configs (.lce) with name, category, author, block count and last modified date.")]
    public string ListConfigs()
    {
        McpUi.Run(() => OB.ConfigManager.Rescan());
        var configs = OB.ConfigManager.Configs.Select(c => new
        {
            name = c.FileName,
            displayName = c.Name,
            category = c.Category,
            author = c.Config.Settings.Author,
            type = c.Config.Settings.Type.ToString(),
            blocks = c.Config.BlocksAmount,
            lastModified = c.Config.Settings.LastModified,
            remote = c.Remote,
            needsProxies = c.Config.Settings.NeedsProxies
        });
        return System.Text.Json.JsonSerializer.Serialize(configs, JsonOpts);
    }

    /// <summary>Gets metadata (and optionally the script) for one config.</summary>
    [McpServerTool(Name = "get_config"),
     Description("Gets one config's metadata by name. Set includeScript=true to also return the full LoliScript body.")]
    public string GetConfig(
        [Description("Config name (file name without .lce)")] string name,
        [Description("Optional category/subfolder")] string category = "",
        [Description("Include the full LoliScript source")] bool includeScript = false)
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}. Use list_configs.";

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            name = vm.FileName,
            displayName = vm.Name,
            category = vm.Category,
            settings = System.Text.Json.JsonSerializer.SerializeToElement(vm.Config.Settings),
            blocks = vm.Config.BlocksAmount,
            script = includeScript ? vm.Config.Script : null
        }, JsonOpts);
    }

    /// <summary>Creates a new EMPTY .lce config — build it with add_block calls.</summary>
    [McpServerTool(Name = "create_config"),
     Description("Creates a new EMPTY OpenBullet CE .lce config. Then build the stack ONE BLOCK AT A TIME with add_block — each call is parse-checked instantly so you catch syntax errors immediately. Do NOT paste whole scripts; use get_block_template + add_block per block, then update_config_settings and debug_config.")]
    public string CreateConfig(
        [Description("Config name (file name without extension)")] string name,
        [Description("Category/subfolder under Configs/ (default 'Default')")] string category = "Default",
        [Description("Author name")] string author = "MCP")
    {
        try { return McpConfigHelper.Create(name, category, author, ""); }
        catch (Exception ex) { return $"Failed to create config: {ex.Message}"; }
    }

    /// <summary>Updates metadata fields (author, version, image, info).</summary>
    [McpServerTool(Name = "update_config_metadata"),
     Description("Updates config metadata: author, version, base64 image icon, additional info. Pass empty string to leave unchanged.")]
    public string UpdateConfigMetadata(
        [Description("Config name")] string name,
        [Description("Author")] string author = "",
        [Description("Version string")] string version = "",
        [Description("Base64 PNG/JPEG icon")] string base64Image = "",
        [Description("Additional info text")] string additionalInfo = "",
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        var s = vm.Config.Settings;
        if (!string.IsNullOrEmpty(author)) s.Author = author;
        if (!string.IsNullOrEmpty(version)) s.Version = version;
        if (!string.IsNullOrEmpty(base64Image)) s.Base64Image = base64Image;
        if (!string.IsNullOrEmpty(additionalInfo)) s.AdditionalInfo = additionalInfo;

        McpConfigHelper.Save(vm);
        return $"Updated metadata for '{name}'";
    }

    /// <summary>Gets the full ConfigSettings object of a config.</summary>
    [McpServerTool(Name = "get_config_settings"),
     Description("Returns the full settings object of a config (suggestedBots, maxCPM, needsProxies, allowedWordlist1/2, dataRules, customInputs, selenium flags, etc.).")]
    public string GetConfigSettings(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";
        return System.Text.Json.JsonSerializer.Serialize(vm.Config.Settings, JsonOpts);
    }

    /// <summary>Updates config settings via a JSON patch object.</summary>
    [McpServerTool(Name = "update_config_settings"),
     Description("Patches config settings with a JSON object, e.g. {\"needsProxies\": true, \"suggestedBots\": 50, \"allowedWordlist1\": \"Default\"}. Field names match ConfigSettings properties from get_config_settings.")]
    public string UpdateConfigSettings(
        [Description("Config name")] string name,
        [Description("JSON object of setting:value")] string settingsJson,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        try
        {
            var jObj = JObject.FromObject(vm.Config.Settings);
            var patch = JObject.Parse(settingsJson);
            jObj.Merge(patch, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            JsonConvert.PopulateObject(jObj.ToString(), vm.Config.Settings);
            McpConfigHelper.Save(vm);
            return $"Updated settings for '{name}'";
        }
        catch (Exception ex) { return $"Failed to update settings: {ex.Message}"; }
    }

    /// <summary>Convenience: set the most common config settings in one call.</summary>
    [McpServerTool(Name = "set_config_settings"),
     Description("One-call setter for the common config settings. All parameters optional — pass only what you want to change. For anything else use update_config_settings.")]
    public string SetConfigSettings(
        [Description("Config name")] string name,
        [Description("Category")] string category = "",
        [Description("Suggested bots")] int suggestedBots = -1,
        [Description("Max CPM (0 = unlimited)")] int maxCpm = -1,
        [Description("Whether the config needs proxies")] bool? needsProxies = null,
        [Description("Only SOCKS proxies")] bool? onlySocks = null,
        [Description("Only SSL proxies")] bool? onlySsl = null,
        [Description("Max uses per proxy")] int maxProxyUses = -1,
        [Description("Ban proxy after good status")] bool? banProxyAfterGoodStatus = null,
        [Description("Ban loop evasion override (0 = use global)")] int banLoopEvasionOverride = -1,
        [Description("Allowed wordlist type 1 (from get_environment_info)")] string allowedWordlist1 = "",
        [Description("Allowed wordlist type 2")] string allowedWordlist2 = "",
        [Description("Continue on custom status")] bool? continueOnCustom = null,
        [Description("Save hits to text file")] bool? saveHitsToTextFile = null,
        [Description("Save empty captures")] bool? saveEmptyCaptures = null,
        [Description("Max redirects for requests")] int maxRedirects = -1)
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        var s = vm.Config.Settings;
        if (suggestedBots >= 0) s.SuggestedBots = suggestedBots;
        if (maxCpm >= 0) s.MaxCPM = maxCpm;
        if (needsProxies.HasValue) s.NeedsProxies = needsProxies.Value;
        if (onlySocks.HasValue) s.OnlySocks = onlySocks.Value;
        if (onlySsl.HasValue) s.OnlySsl = onlySsl.Value;
        if (maxProxyUses >= 0) s.MaxProxyUses = maxProxyUses;
        if (banProxyAfterGoodStatus.HasValue) s.BanProxyAfterGoodStatus = banProxyAfterGoodStatus.Value;
        if (banLoopEvasionOverride >= 0) s.BanLoopEvasionOverride = banLoopEvasionOverride;
        if (!string.IsNullOrEmpty(allowedWordlist1)) s.AllowedWordlist1 = allowedWordlist1;
        if (!string.IsNullOrEmpty(allowedWordlist2)) s.AllowedWordlist2 = allowedWordlist2;
        if (continueOnCustom.HasValue) s.ContinueOnCustom = continueOnCustom.Value;
        if (saveHitsToTextFile.HasValue) s.SaveHitsToTextFile = saveHitsToTextFile.Value;
        if (saveEmptyCaptures.HasValue) s.SaveEmptyCaptures = saveEmptyCaptures.Value;
        if (maxRedirects >= 0) s.MaxRedirects = maxRedirects;

        McpConfigHelper.Save(vm);
        return $"Settings updated for '{name}'";
    }

    /// <summary>Gets the readme (markdown sidecar file) of a config.</summary>
    [McpServerTool(Name = "get_config_readme"),
     Description("Gets the readme/documentation of a config. Stored as a .md file next to the .lce file.")]
    public string GetConfigReadme(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";
        var mdPath = Path.ChangeExtension(McpConfigHelper.GetPath(vm), ".md");
        return File.Exists(mdPath) ? File.ReadAllText(mdPath) : "";
    }

    /// <summary>Writes the readme of a config.</summary>
    [McpServerTool(Name = "update_config_readme"),
     Description("Writes the readme/documentation of a config (markdown). Stored as a .md sidecar file next to the .lce.")]
    public string UpdateConfigReadme(
        [Description("Config name")] string name,
        [Description("Markdown content")] string readme,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";
        var mdPath = Path.ChangeExtension(McpConfigHelper.GetPath(vm), ".md");
        File.WriteAllText(mdPath, readme);
        return $"Readme saved ({mdPath})";
    }

    /// <summary>Deletes a config (removes the .lce file).</summary>
    [McpServerTool(Name = "delete_config"),
     Description("Deletes a config file permanently.")]
    public string DeleteConfig(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.Find(name, category);
        if (vm == null) return $"Config not found: {name}";

        try
        {
            var path = McpConfigHelper.GetPath(vm);
            McpUi.Run(() => OB.ConfigManager.Remove(vm));
            if (File.Exists(path)) File.Delete(path);
            return $"Deleted config '{name}'";
        }
        catch (Exception ex) { return $"Failed to delete config: {ex.Message}"; }
    }

    /// <summary>Rescans the Configs folder — picks up files added externally.</summary>
    [McpServerTool(Name = "reload_configs"),
     Description("Rescans the Configs folder, picking up any .lce files added or changed outside the app.")]
    public string ReloadConfigs()
    {
        McpUi.Run(() => OB.ConfigManager.Rescan());
        return $"Rescanned. {OB.ConfigManager.Total} configs found.";
    }

    /// <summary>Reads a config fresh from disk, bypassing the in-memory copy.</summary>
    [McpServerTool(Name = "get_fresh_config"),
     Description("Reads a config straight from its .lce file on disk, bypassing any in-memory copy. Use when you edited the file externally.")]
    public string GetFreshConfig(
        [Description("Config name")] string name,
        [Description("Category")] string category = "")
    {
        var vm = McpConfigHelper.FindFresh(name, category);
        if (vm == null) return $"Config not found: {name}";
        return vm.Config.Script;
    }
}
