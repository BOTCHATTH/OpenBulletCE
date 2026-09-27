using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenBulletCE.Models;
using RuriLib.Enums;
using RuriLib.Runner;
using RuriLib.ViewModels;
using System;

namespace OpenBulletCE.Views.Pages;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        Populate(BotsDisplayModeCombo, typeof(BotsDisplayMode));
        Populate(CaptchaServiceCombo, typeof(CaptchaServiceType));
        Populate(SeleniumBrowserCombo, typeof(BrowserType));
    }

    private static void Populate(ComboBox combo, Type enumType)
    {
        foreach (var v in Enum.GetValues(enumType))
            combo.Items.Add(v);
    }

    // Auth combo lives inside an ItemsControl DataTemplate — populate per-instance.
    private void AuthCombo_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is ComboBox combo && combo.Items.Count == 0)
            Populate(combo, typeof(Source.AuthMode));
    }

    private async void ChromePath_Click(object? sender, RoutedEventArgs e)
        => await PickBinary(p => OB.Settings.RLSettings.Selenium.ChromeBinaryLocation = p);

    private async void FirefoxPath_Click(object? sender, RoutedEventArgs e)
        => await PickBinary(p => OB.Settings.RLSettings.Selenium.FirefoxBinaryLocation = p);

    private async void BackgroundImagePick_Click(object? sender, RoutedEventArgs e)
        => await PickImage(p => OB.OBSettings.Themes.BackgroundImage = p);

    private async void BackgroundLogoPick_Click(object? sender, RoutedEventArgs e)
        => await PickImage(p => OB.OBSettings.Themes.BackgroundLogo = p);

    private static async System.Threading.Tasks.Task PickImage(Action<string> set)
    {
        if (Alerter.Dialogs == null) return;
        var files = await Alerter.Dialogs.OpenFilePickerAsync(
            "Select Image", "Images", "png", "jpg", "jpeg", "bmp", "gif", "webp");
        if (files == null || files.Length == 0) return;
        set(files[0]);
    }

    private static async System.Threading.Tasks.Task PickBinary(Action<string> set)
    {
        if (Alerter.Dialogs == null) return;
        var files = await Alerter.Dialogs.OpenFilePickerAsync("Select Browser Executable", "Executables", "exe");
        if (files == null || files.Length == 0) return;
        set(files[0]);
    }
}
