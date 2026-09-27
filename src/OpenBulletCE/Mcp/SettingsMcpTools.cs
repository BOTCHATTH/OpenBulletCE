using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RuriLib;
using RuriLib.Models;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for reading and updating application settings.
/// </summary>
[McpServerToolType]
public sealed class SettingsMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Returns the current OpenBullet CE settings (General, Themes, MCP).</summary>
    [McpServerTool(Name = "get_settings"),
     Description("Returns the current OpenBullet CE settings (general, themes, MCP server settings).")]
    public string GetSettings()
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            general = new
            {
                displayLoliScriptOnLoad = OB.OBSettings.General.DisplayLoliScriptOnLoad,
                recommendedBots = OB.OBSettings.General.RecommendedBots,
                startingWidth = OB.OBSettings.General.StartingWidth,
                startingHeight = OB.OBSettings.General.StartingHeight,
            },
            mcp = new
            {
                enabled = OB.OBSettings.Mcp.Enabled,
                port = OB.OBSettings.Mcp.Port,
                bindAddress = OB.OBSettings.Mcp.BindAddress,
                apiKeySet = !string.IsNullOrEmpty(OB.OBSettings.Mcp.ApiKey),
                status = McpServerHost.Status.ToString()
            }
        }, JsonOpts);
    }

    /// <summary>
    /// Updates top-level OpenBullet CE settings. Pass a JSON object whose keys
    /// match OBSettingsGeneral properties (e.g. {"recommendedBots": false}).
    /// </summary>
    [McpServerTool(Name = "update_settings"),
     Description("Updates OpenBullet CE settings. Pass a JSON object of property:value pairs matching OBSettingsGeneral fields, e.g. {\"recommendedBots\": false}. Settings persist to OBSettings.json.")]
    public string UpdateSettings(
        [Description("JSON object of setting name to value")] string settingsJson)
    {
        try
        {
            var node = JsonNode.Parse(settingsJson)?.AsObject()
                ?? throw new ArgumentException("settingsJson must be a JSON object");

            var general = OB.OBSettings.General;
            var jObj = JObject.FromObject(general);
            foreach (var kv in node)
                jObj[kv.Key!] = kv.Value?.ToJsonString() is string s ? JToken.Parse(s) : null;

            McpUi.Run(() => JsonConvert.PopulateObject(jObj.ToString(), general));
            OBIOManager.SaveSettings(OB.obSettingsFile, OB.OBSettings);

            return $"Settings updated and saved to {OB.obSettingsFile}";
        }
        catch (Exception ex) { return $"Failed to update settings: {ex.Message}"; }
    }

    /// <summary>Returns the RuriLib runner settings (bots behaviour, proxies, captcha, selenium).</summary>
    [McpServerTool(Name = "get_rurilib_settings"),
     Description("Returns the RuriLib settings used by the runner engine (general, proxies, captchas, selenium sections).")]
    public string GetRuriLibSettings()
    {
        var s = OB.Settings.RLSettings;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            general = new
            {
                waitTime = s.General.WaitTime,
                requestTimeout = s.General.RequestTimeout,
                maxHits = s.General.MaxHits,
                enableBotLog = s.General.EnableBotLog,
                saveLastSource = s.General.SaveLastSource,
                sendToCheckOnAbort = s.General.SendToCheckOnAbort,
                webhookEnabled = s.General.WebhookEnabled,
                webhookURL = s.General.WebhookURL,
                webhookUser = s.General.WebhookUser
            },
            proxies = new
            {
                concurrentUse = s.Proxies.ConcurrentUse,
                neverBan = s.Proxies.NeverBan,
                banLoopEvasion = s.Proxies.BanLoopEvasion,
                shuffleOnStart = s.Proxies.ShuffleOnStart,
                reload = s.Proxies.Reload,
                reloadSource = s.Proxies.ReloadSource.ToString(),
                reloadPath = s.Proxies.ReloadPath,
                reloadType = s.Proxies.ReloadType.ToString(),
                reloadInterval = s.Proxies.ReloadInterval,
                alwaysGetClearance = s.Proxies.AlwaysGetClearance,
                globalBanKeys = s.Proxies.GlobalBanKeys,
                globalRetryKeys = s.Proxies.GlobalRetryKeys,
                remoteProxySources = s.Proxies.RemoteProxySources
            },
            captchas = s.Captchas,
            selenium = s.Selenium
        }, JsonOpts);
    }

    /// <summary>
    /// Updates RuriLib settings by JSON path, e.g. {"general.waitTime": 1000}
    /// or a nested object {"proxies": {"neverBan": true}}.
    /// </summary>
    [McpServerTool(Name = "update_rurilib_settings"),
     Description("Updates RuriLib runner settings. Pass either a nested JSON object matching the structure from get_rurilib_settings, or dotted keys like {\"proxies.neverBan\": true}. Settings persist to RLSettings.json.")]
    public string UpdateRuriLibSettings(
        [Description("JSON object of settings (nested or dotted keys)")] string settingsJson)
    {
        try
        {
            var node = JsonNode.Parse(settingsJson)?.AsObject()
                ?? throw new ArgumentException("settingsJson must be a JSON object");

            var jObj = JObject.FromObject(OB.Settings.RLSettings);

            foreach (var kv in node)
            {
                var key = kv.Key!;
                var value = kv.Value?.ToJsonString() is string s ? JToken.Parse(s) : null;

                if (key.Contains('.'))
                {
                    var parts = key.Split('.');
                    JToken? current = jObj;
                    for (int i = 0; i < parts.Length - 1; i++)
                        current = current?[parts[i]];
                    if (current is JObject parent) parent[parts[^1]] = value;
                }
                else if (value is JObject nested && jObj[key] is JObject existing)
                {
                    existing.Merge(nested, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                }
                else
                {
                    jObj[key] = value;
                }
            }

            McpUi.Run(() => JsonConvert.PopulateObject(jObj.ToString(), OB.Settings.RLSettings));
            IOManager.SaveSettings(OB.rlSettingsFile, OB.Settings.RLSettings);

            return $"RuriLib settings updated and saved to {OB.rlSettingsFile}";
        }
        catch (Exception ex) { return $"Failed to update RuriLib settings: {ex.Message}"; }
    }
}
