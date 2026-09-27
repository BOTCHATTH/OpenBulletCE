using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenBulletCE.Plugins;
using OpenBulletCE.ViewModels.Pages;

namespace OpenBulletCE.Views.Pages;

public partial class PluginsView : UserControl
{
    public PluginsView()
    {
        InitializeComponent();
    }

    private PluginsPageViewModel Page => DataContext as PluginsPageViewModel;

    private async void AddPlugin_Click(object? sender, RoutedEventArgs e)
    {
        if (Alerter.Dialogs == null || Page == null) return;
        var files = await Alerter.Dialogs.OpenFilePickerAsync("Add Plugin", "Plugin DLLs", "dll");
        if (files.Length > 0)
            Page.AddPlugin(files[0]);
    }

    private void RemovePlugin_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PluginDescriptor d)
            Page?.RemovePlugin(d);
    }

    private void PluginCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border b && b.Tag is PluginDescriptor d && Page != null)
            Page.SelectedPlugin = d;
    }

    private void PluginMethod_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PluginMethodDescriptor method)
        {
            method.Invoke();
        }
    }

    private async void FilePicker_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PluginPropertyDescriptor prop)
        {
            if (Alerter.Dialogs != null)
            {
                var filters = string.IsNullOrEmpty(prop.Filter)
                    ? new[] { "*.*" }
                    : prop.Filter.Split('|');
                var files = await Alerter.Dialogs.OpenFilePickerAsync("Select File", "Files", filters);
                if (files.Length > 0)
                    prop.Value = files[0];
            }
        }
    }
}
