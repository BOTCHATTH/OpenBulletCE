using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenBulletCE.ViewModels.Pages;
using OpenBulletCE.Views.Dialogs;
using RuriLib.Enums;
using RuriLib.Models;
using RuriLib.ViewModels;
using System.Linq;

namespace OpenBulletCE.Views.Pages;

public partial class ConfigView : UserControl
{
    public ConfigView()
    {
        InitializeComponent();
    }

    private ConfigPageViewModel? VM => DataContext as ConfigPageViewModel;

    private DataGrid? _configsGrid;
    private TextBox? _filterBox;

    private void ConfigsGrid_Loaded(object? sender, RoutedEventArgs e) => _configsGrid = sender as DataGrid;
    private void ConfigFilter_Loaded(object? sender, RoutedEventArgs e) => _filterBox = sender as TextBox;

    private void OpenStacker()
    {
        if (VM?.SelectedConfig == null) return;
        VM.SwitchToStackerCommand.Execute(null);
    }

    private void EditConfig_Click(object? sender, RoutedEventArgs e) => OpenStacker();

    private void ConfigsGrid_DoubleTapped(object? sender, TappedEventArgs e) => OpenStacker();

    private void SaveConfig_Click(object? sender, RoutedEventArgs e)
    {
        try { OB.ConfigManager.SaveCurrent(); }
        catch (System.Exception ex) { OB.Logger.LogError(Components.ConfigManager, ex.Message); }
    }

    private void DeleteConfigs_Click(object? sender, RoutedEventArgs e)
    {
        var selected = _configsGrid?.SelectedItems?.Cast<ConfigViewModel>().ToList();
        if (selected == null || selected.Count == 0) return;
        VM?.DeleteConfigs(selected);
    }

    private void OpenConfigFolder_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.IO.Directory.CreateDirectory(OB.configFolder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(OB.configFolder) { UseShellExecute = true });
        }
        catch { }
    }

    private void SearchConfigs_Click(object? sender, RoutedEventArgs e)
    {
        if (VM != null && _filterBox != null)
            VM.SearchString = _filterBox.Text ?? "";
    }

    private void ConfigFilter_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter) SearchConfigs_Click(sender, e);
    }

    private void ConfigsGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (VM != null && _configsGrid?.SelectedItem is ConfigViewModel cfg)
        {
            OB.ConfigManager.HoveredConfig = cfg.Config;
            VM.SelectedConfig = cfg;
        }
    }

    private async void NewConfig_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogNewConfig();
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window w)
        {
            var result = await dialog.ShowDialog<bool>(w);
            if (result)
            {
                var name = dialog.ConfigName;
                var category = dialog.Category;
                var author = dialog.Author;
                var type = dialog.ConfigType == "CookieEdition" ? ConfigType.CookieEdition : ConfigType.Default;

                try
                {
                    OB.ConfigManager.CreateConfig(name, type, category, author);
                }
                catch (System.Exception ex)
                {
                    await Alerter.Dialogs.AlertAsync(ex.Message, "Error");
                }
            }
        }
    }

    // ═══ Other Options ═══

    private OtherOptionsContent? Oo => VM?.ActiveContent as OtherOptionsContent;

    private void OoTab_Click(object? sender, RoutedEventArgs e)
    {
        if (Oo != null && sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out var tab))
            Oo.OoTab = tab;
    }

    private void OoWlType_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox cb || Oo?.ConfigSettings == null) return;
        cb.Items.Clear();
        foreach (var t in OB.Settings.Environment.WordlistTypes)
            cb.Items.Add(t.Name);
        var current = cb.Tag?.ToString() == "1"
            ? Oo.ConfigSettings.AllowedWordlist1
            : Oo.ConfigSettings.AllowedWordlist2;
        cb.SelectedItem = current;
    }

    private void OoWlType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb || Oo?.ConfigSettings == null) return;
        if (cb.Tag?.ToString() == "1")
            Oo.ConfigSettings.AllowedWordlist1 = cb.SelectedItem?.ToString() ?? "";
        else
            Oo.ConfigSettings.AllowedWordlist2 = cb.SelectedItem?.ToString() ?? "";
    }

    private void OoAddInput_Click(object? sender, RoutedEventArgs e)
    {
        var s = Oo?.ConfigSettings;
        if (s == null) return;
        var id = s.CustomInputs.Count == 0 ? 0 : s.CustomInputs.Max(i => i.Id) + 1;
        s.CustomInputs.Add(new CustomInput(id) { Description = "Input Description", VariableName = "INPUT" });
    }

    private void OoClearInputs_Click(object? sender, RoutedEventArgs e)
        => Oo?.ConfigSettings?.CustomInputs.Clear();

    private void OoRemoveInput_Click(object? sender, RoutedEventArgs e)
    {
        if (Oo?.ConfigSettings != null && sender is Button b && int.TryParse(b.Tag?.ToString(), out var id))
            Oo.ConfigSettings.RemoveCustomInputById(id);
    }

    private async void OoLoadImage_Click(object? sender, RoutedEventArgs e)
    {
        var files = await Alerter.Dialogs.OpenFilePickerAsync("Select Image", "Images", "png", "jpg", "jpeg", "gif", "bmp", "webp");
        if (files == null || files.Length == 0) return;
        try
        {
            var bytes = System.IO.File.ReadAllBytes(files[0]);
            Oo!.Base64Image = System.Convert.ToBase64String(bytes);
        }
        catch (System.Exception ex)
        {
            OB.Logger.LogError(Components.ConfigManager, $"Could not load image: {ex.Message}");
        }
    }

    private void OoClearImage_Click(object? sender, RoutedEventArgs e)
    {
        if (Oo != null) Oo.Base64Image = "";
    }

    private async void OoLoadImagePath_Click(object? sender, RoutedEventArgs e)
    {
        var path = Oo?.ImagePath?.Trim().Trim('"');
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            byte[] bytes;
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Download the image when a URL is provided instead of a file path
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                bytes = await http.GetByteArrayAsync(uri);
            }
            else
            {
                if (!System.IO.File.Exists(path)) return;
                bytes = System.IO.File.ReadAllBytes(path);
            }
            Oo.Base64Image = System.Convert.ToBase64String(bytes);
        }
        catch (System.Exception ex)
        {
            OB.Logger.LogError(Components.ConfigManager, $"Could not load image: {ex.Message}");
        }
    }

    private void OoAddRule_Click(object? sender, RoutedEventArgs e)
    {
        var s = Oo?.ConfigSettings;
        if (s == null) return;
        var id = s.DataRules.Count == 0 ? 0 : s.DataRules.Max(r => r.Id) + 1;
        s.DataRules.Add(new DataRule(id) { SliceName = "DATA", RuleString = "" });
    }

    private void OoClearRules_Click(object? sender, RoutedEventArgs e)
        => Oo?.ConfigSettings?.DataRules.Clear();

    private void OoRemoveRule_Click(object? sender, RoutedEventArgs e)
    {
        if (Oo?.ConfigSettings != null && sender is Button b && int.TryParse(b.Tag?.ToString(), out var id))
            Oo.ConfigSettings.RemoveDataRuleById(id);
    }
}
