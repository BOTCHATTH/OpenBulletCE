using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace OpenBulletCE.Views.Pages;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }

    private void Repo_Click(object? sender, RoutedEventArgs e)
        => OpenUrl("https://github.com/BOTCHATTH/OpenBulletCE");

    private void Docs_Click(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var dialog = new Dialogs.DialogUsageGuide();
        if (window != null) dialog.Show(window);
        else dialog.Show();
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }
}
