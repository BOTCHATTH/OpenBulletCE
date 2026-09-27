using Avalonia.Controls;
using Avalonia.Input;
using OpenBulletCE.ViewModels.Pages;
using OpenBulletCE.Views.Dialogs;
using RuriLib.Models;
using System;
using System.Linq;

namespace OpenBulletCE.Views.Pages;

public partial class HitsDBView : UserControl
{
    public HitsDBView()
    {
        InitializeComponent();
        foreach (var t in new[] { "SUCCESS", "CUSTOM", "NONE", "FAIL", "RETRY", "BAN", "ERROR" })
            TypeFilterCombo.Items.Add(t);
    }

    private HitsDBPageViewModel Page => DataContext as HitsDBPageViewModel;

    // The grid is named HitsGrid in axaml — menu handlers reference it
    // directly instead of walking the Parent chain from the MenuItem.
    private DataGrid GetGrid(object sender) => HitsGrid;

    private void HitsGrid_DoubleTapped(object sender, TappedEventArgs e)
    {
        var grid = sender as DataGrid;
        if (grid?.SelectedItem is not Hit hit) return;

        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var dialog = new DialogShowLog();
        dialog.Show(window);
    }

    private void CopyData_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var grid = GetGrid(sender);
        if (grid?.SelectedItems == null) return;
        CopyToClipboard(string.Join(Environment.NewLine, grid.SelectedItems.Cast<Hit>().Select(h => h.Data)));
    }

    private void CopyProxy_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var grid = GetGrid(sender);
        if (grid?.SelectedItem is not Hit hit) return;
        CopyToClipboard(hit.Proxy ?? "");
    }

    private void CopyCapture_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var grid = GetGrid(sender);
        if (grid?.SelectedItems == null) return;
        CopyToClipboard(string.Join(Environment.NewLine, grid.SelectedItems.Cast<Hit>().Select(h => $"{h.Data} | {h.CapturedString}")));
    }

    private void DeleteSelected_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var grid = GetGrid(sender);
        if (grid?.SelectedItems == null || Page == null) return;
        var selected = grid.SelectedItems.Cast<Hit>().ToList();
        foreach (var hit in selected)
            Page.Manager.Remove(hit);
    }

    private void SelectAll_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        GetGrid(sender)?.SelectAll();
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
