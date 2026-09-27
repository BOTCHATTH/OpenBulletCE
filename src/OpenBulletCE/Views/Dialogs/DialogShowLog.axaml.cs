using Avalonia.Controls;
using Avalonia.Interactivity;
using RuriLib;
using System.Collections.Specialized;
using System.Linq;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogShowLog : Window
{
    public DialogShowLog()
    {
        InitializeComponent();
        RefreshGrid();

        if (OB.Logger?.EntriesCollection != null)
            ((INotifyCollectionChanged)OB.Logger.EntriesCollection).CollectionChanged += (s, e) => RefreshGrid();
    }

    private void RefreshGrid()
    {
        var filter = FilterTextBox?.Text ?? "";
        var items = (OB.Logger?.EntriesCollection ?? Enumerable.Empty<LogEntry>())
            .Where(l => string.IsNullOrEmpty(filter)
                || l.LogString.Contains(filter, System.StringComparison.OrdinalIgnoreCase)
                || l.LogComponent.Contains(filter, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        LogGrid.ItemsSource = items;
        if (items.Count > 0)
            LogGrid.ScrollIntoView(items[^1], null);
    }

    private void Filter_Changed(object? sender, TextChangedEventArgs e) => RefreshGrid();

    private void Clear_Click(object? sender, RoutedEventArgs e)
    {
        OB.Logger?.Clear();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
