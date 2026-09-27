using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogUsageGuide : Window
{
    public DialogUsageGuide()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
