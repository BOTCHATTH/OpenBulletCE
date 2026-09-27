using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenBulletCE.ViewModels.Pages;
using OpenBulletCE.Views.Dialogs;
using RuriLib.Models;
using System;
using System.IO;
using System.Linq;

namespace OpenBulletCE.Views.Pages;

public partial class ProxyView : UserControl
{
    public ProxyView()
    {
        InitializeComponent();
    }

    private ProxyPageViewModel Page => DataContext as ProxyPageViewModel;

    private async void AddProxiesDialog_Click(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var dialog = new DialogAddProxies();
        await dialog.ShowDialog<bool>(window);
    }

    private void CopyProxies_Click(object? sender, RoutedEventArgs e)
    {
        var grid = ProxiesGrid;
        if (grid?.SelectedItems == null) return;

        var text = string.Join(Environment.NewLine,
            grid.SelectedItems.Cast<CProxy>().Select(p => p.Proxy));
        CopyToClipboard(text);
    }

    private void CopyProxiesFull_Click(object? sender, RoutedEventArgs e)
    {
        var grid = ProxiesGrid;
        if (grid?.SelectedItems == null) return;

        var text = string.Join(Environment.NewLine,
            grid.SelectedItems.Cast<CProxy>().Select(p => p.ToString()));
        CopyToClipboard(text);
    }

    private void DeleteSelected_Click(object? sender, RoutedEventArgs e)
    {
        var grid = ProxiesGrid;
        if (grid?.SelectedItems == null || Page == null) return;

        var selected = grid.SelectedItems.Cast<CProxy>().ToList();
        Page.Manager.Remove(selected);
    }

    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || Page == null) return;

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Proxies",
            SuggestedFileName = "proxies.txt",
            DefaultExtension = "txt"
        });

        if (file == null) return;

        var text = string.Join(Environment.NewLine, Page.Proxies.Select(p => p.ToString()));
        await using var stream = await file.OpenWriteAsync();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text);
    }

    private void CopyToClipboard(string text)
    {
        try
        {
            var clipboard = (TopLevel.GetTopLevel(this) as Window)?.Clipboard;
            clipboard?.SetTextAsync(text);
        }
        catch { }
    }
}
