using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RuriLib.Models;
using System.Linq;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogSelectWordlist : Window
{
    public Wordlist SelectedWordlist { get; private set; }

    public DialogSelectWordlist()
    {
        InitializeComponent();
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        var filter = FilterTextBox?.Text ?? "";
        var items = OB.WordlistManager.Wordlists
            .Where(w => string.IsNullOrEmpty(filter) || w.Name.Contains(filter, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        WordlistsGrid.ItemsSource = items;
    }

    private void Filter_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
            RefreshGrid();
    }

    private void Grid_DoubleTapped(object? sender, TappedEventArgs e) => DoSelect();

    private void Select_Click(object? sender, RoutedEventArgs e) => DoSelect();

    private void DoSelect()
    {
        if (WordlistsGrid.SelectedItem is Wordlist wl)
        {
            SelectedWordlist = wl;
            Close(true);
        }
    }

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        if (Alerter.Dialogs != null)
        {
            var files = await Alerter.Dialogs.OpenFilePickerAsync("Select Wordlist", "Text files", "txt", "csv", "lst");
            if (files.Length > 0)
            {
                try
                {
                    var wl = ViewModels.WordlistManagerViewModel.FileToWordlist(files[0]);
                    OB.WordlistManager.Add(wl);
                    RefreshGrid();
                }
                catch (System.Exception ex)
                {
                    await Alerter.Dialogs.AlertAsync(ex.Message, "Error");
                }
            }
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
