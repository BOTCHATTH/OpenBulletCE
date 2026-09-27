using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogSelectBots : Window
{
    public int Bots { get; private set; } = 1;

    public DialogSelectBots() : this(200) { }

    public DialogSelectBots(int maxBots)
    {
        InitializeComponent();
        BotsInput.Maximum = maxBots;
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        Bots = (int)(BotsInput.Value ?? 1);
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
