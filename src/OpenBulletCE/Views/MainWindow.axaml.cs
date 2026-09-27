using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using OpenBulletCE.Views.Dialogs;
using System;

namespace OpenBulletCE.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Custom chrome (ExtendClientArea + NoChrome): when maximized Windows
        // parks the frame 8px off-screen for invisible resize borders, which
        // shifts all content left/up — push it back via OffScreenMargin.
        if (change.Property == WindowStateProperty)
            RootGrid.Margin = WindowState == WindowState.Maximized ? OffScreenMargin : new Thickness(0);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        ApplyThemeImages();
        if (OB.OBSettings?.Themes != null)
            OB.OBSettings.Themes.PropertyChanged += (s, a) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(ApplyThemeImages);
    }

    private void ApplyThemeImages()
    {
        var t = OB.OBSettings?.Themes;
        if (t == null) return;

        var customBg = t.UseImage ? LoadImage(t.BackgroundImage) : null;
        var bg = customBg ?? LoadDefaultBackground();
        ThemeBackgroundImage.Source = bg;
        // Custom image -> user opacity; built-in theme -> subtle default dim
        ThemeBackgroundImage.Opacity = customBg != null
            ? Math.Clamp(t.BackgroundImageOpacity, 0, 100) / 100.0
            : 0.45;
        ThemeScrim.IsVisible = bg != null;
        ThemeLogoImage.Source = LoadImage(t.UseImage ? t.BackgroundLogo : null);
        ThemeLogoImage.Opacity = 0.12;
    }

    private static Avalonia.Media.Imaging.Bitmap _defaultBg;

    private static Avalonia.Media.Imaging.Bitmap LoadDefaultBackground()
    {
        if (_defaultBg != null) return _defaultBg;
        try
        {
            var uri = new Uri("avares://OpenBulletCE/Assets/default-bg.png");
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            _defaultBg = new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch { }
        return _defaultBg;
    }

    private static Avalonia.Media.Imaging.Bitmap LoadImage(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                return new Avalonia.Media.Imaging.Bitmap(path);
        }
        catch { }
        return null;
    }

    private void TitleBarDrag_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void ShowLog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new DialogShowLog();
        dialog.Show(this);
    }

    private async void Screenshot_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            System.IO.Directory.CreateDirectory(dir);
            var file = System.IO.Path.Combine(dir, $"OB_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(
                new Avalonia.PixelSize((int)ClientSize.Width, (int)ClientSize.Height),
                new Avalonia.Vector(96, 96));
            bitmap.Render(this);
            bitmap.Save(file);
        }
        catch { }
    }
}
