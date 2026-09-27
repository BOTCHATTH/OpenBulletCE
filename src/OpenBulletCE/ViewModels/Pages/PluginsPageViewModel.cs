using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenBulletCE.Plugins;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace OpenBulletCE.ViewModels.Pages;

public partial class PluginsPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<PluginDescriptor> _plugins = new();

    [ObservableProperty]
    private PluginDescriptor _selectedPlugin;

    [ObservableProperty]
    private ObservableCollection<PluginFramework.IBlockPlugin> _blockPlugins = new();

    public System.Collections.Generic.IEnumerable<RuriLib.Models.Wordlist> Wordlists
        => OB.WordlistManager?.Wordlists ?? Enumerable.Empty<RuriLib.Models.Wordlist>();

    public System.Collections.Generic.IEnumerable<RuriLib.ViewModels.ConfigViewModel> Configs
        => OB.ConfigManager?.Configs ?? Enumerable.Empty<RuriLib.ViewModels.ConfigViewModel>();

    public PluginsPageViewModel()
    {
        LoadPlugins();
    }

    [RelayCommand]
    private void LoadPlugins()
    {
        Plugins.Clear();
        var (descriptors, _) = Loader.LoadPlugins(OB.pluginsFolder);
        foreach (var d in descriptors)
            Plugins.Add(d);
        BlockPlugins.Clear();
        foreach (var p in OB.BlockPlugins)
            BlockPlugins.Add(p);
        SelectedPlugin ??= Plugins.FirstOrDefault();
    }

    /// <summary>Copies a plugin dll into the Plugins folder and rescans.</summary>
    public void AddPlugin(string filePath)
    {
        try
        {
            Directory.CreateDirectory(OB.pluginsFolder);
            var dest = Path.Combine(OB.pluginsFolder, Path.GetFileName(filePath));
            File.Copy(filePath, dest, overwrite: true);
        }
        catch { }
        LoadPlugins();
    }

    /// <summary>Deletes the plugin's source dll and rescans.</summary>
    public void RemovePlugin(PluginDescriptor descriptor)
    {
        if (descriptor == null) return;

        // Refuse to remove assemblies that also provide engine block plugins (inbuilt, e.g. CurlTls)
        if (OB.BlockPlugins.Any(b => b.GetType().Assembly == descriptor.PluginType.Assembly))
        {
            OB.Logger.LogWarning(Components.OtherOptions, $"{descriptor.Name} is inbuilt (provides stacker blocks) and cannot be removed");
            return;
        }

        try
        {
            // Assembly.Location is empty for byte-loaded/single-file assemblies — resolve by name
            var dllPath = Path.Combine(OB.pluginsFolder, descriptor.AssemblyName + ".dll");
            if (File.Exists(dllPath))
            {
                File.Delete(dllPath);
                OB.Logger.LogInfo(Components.OtherOptions, $"Removed plugin {descriptor.Name} ({descriptor.AssemblyName}.dll)");
            }
        }
        catch (Exception ex)
        {
            OB.Logger.LogError(Components.OtherOptions, $"Could not remove plugin {descriptor.Name}: {ex.Message}");
        }
        if (SelectedPlugin == descriptor) SelectedPlugin = null;
        LoadPlugins();
    }

    [RelayCommand]
    private void ReloadPlugins()
    {
        SelectedPlugin = null;
        LoadPlugins();
    }

    public void RunMethod(PluginDescriptor descriptor, string methodName)
    {
        Loader.RunMethod(descriptor, methodName);
    }
}
