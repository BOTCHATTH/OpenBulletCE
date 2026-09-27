using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenBulletCE.Views.Pages;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void ShowChangelog_Click(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var dialog = new Dialogs.DialogChangelog();
        if (window != null) dialog.Show(window);
        else dialog.Show();
    }
}
