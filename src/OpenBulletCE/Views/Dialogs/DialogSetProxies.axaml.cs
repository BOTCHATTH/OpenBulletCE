using Avalonia.Controls;
using Avalonia.Interactivity;
using RuriLib.Runner;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogSetProxies : Window
{
    public ProxyMode SelectedMode { get; private set; } = ProxyMode.Default;

    public DialogSetProxies()
    {
        InitializeComponent();
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        if (OnRadio.IsChecked == true)
            SelectedMode = ProxyMode.On;
        else if (OffRadio.IsChecked == true)
            SelectedMode = ProxyMode.Off;
        else
            SelectedMode = ProxyMode.Default;

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
