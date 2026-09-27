using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenBulletCE.ViewModels;
using RuriLib.Models;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogAddCookie : Window
{
    public DialogAddCookie()
    {
        InitializeComponent();
    }

    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        if (Alerter.Dialogs != null)
        {
            var folder = await Alerter.Dialogs.OpenFolderPickerAsync("Select Cookie Folder");
            if (folder != null)
            {
                PathTextBox.Text = folder;
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                    NameTextBox.Text = System.IO.Path.GetFileName(folder.TrimEnd('\\', '/'));
            }
        }
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        var path = PathTextBox?.Text ?? "";
        var name = NameTextBox?.Text ?? "";
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
        {
            Close(false);
            return;
        }

        try
        {
            var cookie = CookieManagerViewModel.FileToCookielist(path);
            cookie.Name = name;
            OB.CookieManager.Add(cookie);
            OB.Logger.LogInfo(Components.CookieManager, $"Added cookie list {name}");
        }
        catch (System.Exception ex)
        {
            _ = Alerter.Dialogs?.AlertAsync(ex.Message, "Error");
            return;
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
