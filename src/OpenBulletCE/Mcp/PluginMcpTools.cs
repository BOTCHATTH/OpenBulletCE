using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OpenBulletCE.Plugins;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for listing installed plugins and block plugins.
/// </summary>
[McpServerToolType]
public sealed class PluginMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Lists loaded block plugins (LoliScript blocks like CURLREQUEST).</summary>
    [McpServerTool(Name = "list_plugins"),
     Description("Lists loaded LoliScript block plugins (e.g. CURLREQUEST) — these add block keywords usable in configs.")]
    public string ListBlockPlugins()
    {
        var plugins = OB.BlockPlugins.Select(p => new
        {
            name = p.Name,
            color = p.Color,
            type = p.GetType().Name
        });
        return JsonSerializer.Serialize(plugins, JsonOpts);
    }

    /// <summary>Lists all plugin assemblies found in the Plugins folder.</summary>
    [McpServerTool(Name = "list_installed_plugins"),
     Description("Lists all plugin assemblies in the Plugins folder with their exposed methods and properties.")]
    public string ListInstalledPlugins()
    {
        try
        {
            var (descriptors, _) = Loader.LoadPlugins(OB.pluginsFolder);
            var list = descriptors.Select(d => new
            {
                name = d.Name,
                assembly = d.AssemblyName,
                properties = d.Properties.Select(p => p.PropertyName),
                methods = d.Methods.Select(m => m.MethodName)
            });
            return JsonSerializer.Serialize(list, JsonOpts);
        }
        catch (Exception ex) { return $"Failed to list plugins: {ex.Message}"; }
    }
}
