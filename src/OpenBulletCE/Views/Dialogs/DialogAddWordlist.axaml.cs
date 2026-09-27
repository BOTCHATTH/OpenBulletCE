using Avalonia.Controls;
using Avalonia.Interactivity;
using RuriLib.Models;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogAddWordlist : Window
{
    public DialogAddWordlist()
    {
        InitializeComponent();
    }

    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        if (Alerter.Dialogs != null)
        {
            var files = await Alerter.Dialogs.OpenFilePickerAsync("Select Wordlist File", "Text files", "txt", "csv", "lst");
            if (files.Length > 0)
            {
                PathTextBox.Text = files[0];
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                    NameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(files[0]);
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

        var type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Default";
        var purpose = (PurposeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Combo";
        try
        {
            var wl = new Wordlist(name, path, type, purpose);
            OB.WordlistManager.Add(wl);
            OB.Logger.LogInfo(Components.WordlistManager, $"Added wordlist {name}");
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
