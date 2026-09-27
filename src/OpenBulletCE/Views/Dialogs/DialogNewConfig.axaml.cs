using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogNewConfig : Window
{
    public string ConfigName => NameTextBox.Text ?? "";
    public string Category => CategoryBox.Text ?? "Default";
    public string ConfigType => (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Default";
    public string Author => AuthorTextBox.Text ?? "";

    public DialogNewConfig()
    {
        InitializeComponent();

        // Populate categories
        if (OB.ConfigManager != null)
        {
            var categories = OB.ConfigManager.Configs
                .Select(c => c.Category)
                .Distinct()
                .ToArray();
            CategoryBox.ItemsSource = categories;
        }

        AuthorTextBox.Text = OB.OBSettings?.General.DefaultAuthor ?? "";
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ConfigName))
        {
            NameTextBox.Focus();
            return;
        }
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
