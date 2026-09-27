using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RuriLib.ViewModels;
using System.Linq;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogSelectConfig : Window
{
    public ConfigViewModel SelectedConfig { get; private set; }

    public DialogSelectConfig()
    {
        InitializeComponent();
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        var filter = FilterTextBox?.Text ?? "";
        var configs = OB.ConfigManager.Configs
            .Where(c => string.IsNullOrEmpty(filter) || c.Name.Contains(filter, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        ConfigsGrid.ItemsSource = configs;
    }

    private void Filter_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            RefreshGrid();
    }

    private void Grid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        DoSelect();
    }

    private void Select_Click(object? sender, RoutedEventArgs e)
    {
        DoSelect();
    }

    private void DoSelect()
    {
        if (ConfigsGrid.SelectedItem is ConfigViewModel config)
        {
            SelectedConfig = config;
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
