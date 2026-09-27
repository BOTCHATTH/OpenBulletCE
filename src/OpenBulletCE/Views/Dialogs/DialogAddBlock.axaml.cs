using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using RuriLib;
using RuriLib.Blocks;
using System;
using System.Linq;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogAddBlock : Window
{
    public BlockBase SelectedBlock { get; private set; }

    // Ordered default block palette — same types the stack can host, in grid order.
    private static readonly Type[] DefaultBlocks =
    {
        typeof(BlockRequest), typeof(BlockCookieContainer), typeof(BlockUtility),
        typeof(BlockKeycheck), typeof(BlockParse), typeof(BlockFunction),
        typeof(BlockSolveCaptcha), typeof(BlockReportCaptcha), typeof(BlockBypassCF),
        typeof(BlockTCP), typeof(BlockScript), typeof(SBlockNavigate), typeof(SBlockBrowserAction),
        typeof(SBlockElementAction), typeof(SBlockExecuteJS)
    };

    public DialogAddBlock()
    {
        InitializeComponent();
        PopulateDefaults();
        PopulatePlugins();
    }

    private void PopulateDefaults()
    {
        DefaultButtonsGrid.Children.Clear();

        foreach (var type in DefaultBlocks)
        {
            var block = Activator.CreateInstance(type) as BlockBase;
            var button = new Button
            {
                Content = block?.Label ?? type.Name,
                Margin = new Avalonia.Thickness(2),
                Tag = type
            };

            // Paint with the runtime stack color so the preview matches the block exactly
            var map = OB.BlockMappings.FirstOrDefault(m => m.Item1 == type);
            if (map.Item1 != null)
            {
                var c = map.Item3;
                button.Background = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
                button.Foreground = new SolidColorBrush(block?.IsSelenium == true ? Colors.White : Colors.Black);
            }

            button.Click += DefaultButton_Click;
            DefaultButtonsGrid.Children.Add(button);
        }
    }

    private void PopulatePlugins()
    {
        PluginButtonsGrid.Children.Clear();
        if (OB.BlockPlugins == null) return;

        foreach (var plugin in OB.BlockPlugins)
        {
            var button = new Button
            {
                Content = plugin.Name,
                Margin = new Avalonia.Thickness(2),
                Tag = plugin.GetType()
            };

            try
            {
                var color = Color.Parse(plugin.Color);
                button.Background = new SolidColorBrush(color);
                if (plugin.LightForeground)
                    button.Foreground = new SolidColorBrush(Colors.Gainsboro);
            }
            catch { }

            button.Click += PluginButton_Click;
            PluginButtonsGrid.Children.Add(button);
        }
    }

    private void DefaultTab_Click(object? sender, RoutedEventArgs e)
    {
        DefaultPanel.IsVisible = true;
        PluginsPanel.IsVisible = false;
    }

    private void PluginsTab_Click(object? sender, RoutedEventArgs e)
    {
        DefaultPanel.IsVisible = false;
        PluginsPanel.IsVisible = true;
    }

    private void DefaultButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Type type)
        {
            SelectedBlock = Activator.CreateInstance(type) as BlockBase;
            Close(true);
        }
    }

    private void PluginButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Type type)
        {
            SelectedBlock = Activator.CreateInstance(type) as BlockBase;
            Close(true);
        }
    }
}
