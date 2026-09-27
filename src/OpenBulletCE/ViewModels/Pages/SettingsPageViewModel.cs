using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenBulletCE.Models;

namespace OpenBulletCE.ViewModels.Pages;

public partial class SettingsPageViewModel : ViewModelBase
{
    public OBSettingsViewModel OBSettings => OB.OBSettings;
    public GlobalSettings RLSettings => OB.Settings;

    [ObservableProperty]
    private string _selectedSection = "General";

    // ─── Level 1: RuriLib | OpenBullet ───
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRLTabActive), nameof(IsOBTabActive),
        nameof(ShowRLGeneral), nameof(ShowRLProxies), nameof(ShowRLCaptchas), nameof(RLSelenium),
        nameof(ShowOBGeneral), nameof(ShowOBSounds), nameof(ShowOBSources), nameof(ShowOBThemes),
        nameof(RLMenuVisible), nameof(OBMenuVisible))]
    private bool _isRLTab = true;

    public bool IsRLTabActive => IsRLTab;
    public bool IsOBTabActive => !IsRLTab;
    public bool RLMenuVisible => IsRLTab;
    public bool OBMenuVisible => !IsRLTab;

    // ─── Level 2 sections ───
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRLGeneral), nameof(ShowRLProxies), nameof(ShowRLCaptchas), nameof(RLSelenium),
        nameof(IsRLGeneralActive), nameof(IsRLProxiesActive), nameof(IsRLCaptchasActive), nameof(IsRLSeleniumActive))]
    private string _rlSection = "General";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOBGeneral), nameof(ShowOBSounds), nameof(ShowOBSources), nameof(ShowOBThemes),
        nameof(IsOBGeneralActive), nameof(IsOBSoundsActive), nameof(IsOBSourcesActive), nameof(IsOBThemesActive))]
    private string _obSection = "General";

    public bool ShowRLGeneral => IsRLTab && RlSection == "General";
    public bool ShowRLProxies => IsRLTab && RlSection == "Proxies";
    public bool ShowRLCaptchas => IsRLTab && RlSection == "Captchas";
    public bool RLSelenium => IsRLTab && RlSection == "Selenium";

    public bool ShowOBGeneral => !IsRLTab && ObSection == "General";
    public bool ShowOBSounds => !IsRLTab && ObSection == "Sounds";
    public bool ShowOBSources => !IsRLTab && ObSection == "Sources";
    public bool ShowOBThemes => !IsRLTab && ObSection == "Themes";

    public bool IsRLGeneralActive => RlSection == "General";
    public bool IsRLProxiesActive => RlSection == "Proxies";
    public bool IsRLCaptchasActive => RlSection == "Captchas";
    public bool IsRLSeleniumActive => RlSection == "Selenium";

    public bool IsOBGeneralActive => ObSection == "General";
    public bool IsOBSoundsActive => ObSection == "Sounds";
    public bool IsOBSourcesActive => ObSection == "Sources";
    public bool IsOBThemesActive => ObSection == "Themes";

    [RelayCommand]
    private void SwitchTab(string tab) => IsRLTab = tab == "RL";

    [RelayCommand]
    private void SwitchRLSection(string section) => RlSection = section;

    [RelayCommand]
    private void SwitchOBSection(string section) => ObSection = section;

    // ─── Sources ───
    [RelayCommand]
    private void AddSource()
    {
        var nextId = OBSettings.Sources.Sources.Count == 0 ? 0 : OBSettings.Sources.Sources[^1].Id + 1;
        OBSettings.Sources.Sources.Add(new Source(nextId));
    }

    [RelayCommand]
    private void ClearSources()
    {
        OBSettings.Sources.Sources.Clear();
    }

    [RelayCommand]
    private void RemoveSource(Source source)
    {
        if (source != null)
            OBSettings.Sources.Sources.Remove(source);
    }

    // ─── Proxy check targets (OB2 parity: URL + success key lists) ───
    [ObservableProperty]
    private string _newProxySiteUrl = "";

    [ObservableProperty]
    private string _newProxyKey = "";

    public System.Collections.ObjectModel.ObservableCollection<string> ProxySiteUrls
        => OB.Settings.ProxyManagerSettings.ProxySiteUrls;

    public System.Collections.ObjectModel.ObservableCollection<string> ProxyKeys
        => OB.Settings.ProxyManagerSettings.ProxyKeys;

    [RelayCommand]
    private void AddProxySiteUrl()
    {
        if (string.IsNullOrWhiteSpace(NewProxySiteUrl)) return;
        ProxySiteUrls.Add(NewProxySiteUrl.Trim());
        NewProxySiteUrl = "";
    }

    [RelayCommand]
    private void RemoveProxySiteUrl(string url)
    {
        if (url != null) ProxySiteUrls.Remove(url);
    }

    [RelayCommand]
    private void AddProxyKey()
    {
        if (string.IsNullOrWhiteSpace(NewProxyKey)) return;
        ProxyKeys.Add(NewProxyKey.Trim());
        NewProxyKey = "";
    }

    [RelayCommand]
    private void RemoveProxyKey(string key)
    {
        if (key != null) ProxyKeys.Remove(key);
    }

    [RelayCommand]
    private void ResetCustomization()
    {
        OBSettings.Themes.Reset();
    }

    // ─── Save / Reset ───
    [RelayCommand]
    private void SaveSettings()
    {
        OBIOManager.SaveSettings(OB.obSettingsFile, OB.OBSettings);
        RuriLib.IOManager.SaveSettings(OB.rlSettingsFile, OB.Settings.RLSettings);
        RuriLib.IOManager.SaveSettings(OB.proxyManagerSettingsFile, OB.Settings.ProxyManagerSettings);
        OB.Logger.LogInfo(Components.Settings, "Settings saved");
    }

    [RelayCommand]
    private void ResetSettings()
    {
        OB.OBSettings.Reset();
        SaveSettings();
    }
}
