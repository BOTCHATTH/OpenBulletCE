using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RuriLib.Models;
using System.Linq;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogSelectCookie : Window
{
    public Cookie SelectedCookie { get; private set; }

    public DialogSelectCookie()
    {
        InitializeComponent();
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        var filter = FilterTextBox?.Text ?? "";
        var items = OB.CookieManager.Cookies
            .Where(c => string.IsNullOrEmpty(filter) || c.Name.Contains(filter, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        CookiesGrid.ItemsSource = items;
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
        if (CookiesGrid.SelectedItem is Cookie c)
        {
            SelectedCookie = c;
            Close(true);
        }
    }

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        if (Alerter.Dialogs != null)
        {
            var folder = await Alerter.Dialogs.OpenFolderPickerAsync("Select Cookie Folder");
            if (folder != null)
            {
                try
                {
                    var c = ViewModels.CookieManagerViewModel.FileToCookielist(folder);
                    OB.CookieManager.Add(c);
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
