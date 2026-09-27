using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenBulletCE.ViewModels;
using RuriLib;
using RuriLib.Models;
using RuriLib.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBulletCE.ViewModels.Pages;

// Content types for the Configs section tabs
public class ConfigManagerContent { }
public class StackerContent { public StackerViewModel StackerVM { get; set; } }
public partial class OtherOptionsContent : ObservableObject
{
    [ObservableProperty]
    private ConfigSettings _configSettings;

    /// <summary>Current config image as an Avalonia bitmap, decoded from Base64Image.</summary>
    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap _configImage;

    /// <summary>File path typed/pasted for loading an image.</summary>
    [ObservableProperty]
    private string _imagePath = "";

    private string _base64Image = "";
    /// <summary>Mirrors ConfigSettings.Base64Image and refreshes the preview.</summary>
    public string Base64Image
    {
        get => _base64Image;
        set
        {
            _base64Image = value;
            if (ConfigSettings != null) ConfigSettings.Base64Image = value;
            try
            {
                ConfigImage = string.IsNullOrEmpty(value)
                    ? null
                    : new Avalonia.Media.Imaging.Bitmap(new System.IO.MemoryStream(System.Convert.FromBase64String(value)));
            }
            catch { ConfigImage = null; }
        }
    }

    [ObservableProperty]
    private int _ooTab;
}

public enum ConfigTab { Manager, Stacker, OtherOptions }

public partial class ConfigPageViewModel : ViewModelBase
{
    public ConfigManagerViewModel Manager => OB.ConfigManager;

    public ObservableCollection<ConfigViewModel> Configs => Manager.ConfigsCollection;
    public int Total => Manager.Total;
    public string TotalLabel => $"{Manager.Total} configs";
    public string CurrentConfigName => Manager.CurrentConfigName;
    public Config HoveredConfig => Manager.HoveredConfig;

    [ObservableProperty]
    private string _searchString = "";

    [ObservableProperty]
    private ConfigViewModel _selectedConfig;

    [ObservableProperty]
    private string _loliScript = "";

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManagerActive), nameof(IsStackerActive), nameof(IsOtherOptionsActive))]
    private ConfigTab _activeTab = ConfigTab.Manager;

    public bool IsManagerActive => ActiveTab == ConfigTab.Manager;
    public bool IsStackerActive => ActiveTab == ConfigTab.Stacker;
    public bool IsOtherOptionsActive => ActiveTab == ConfigTab.OtherOptions;

    [ObservableProperty]
    private object _activeContent;

    private readonly ConfigManagerContent _managerContent = new();
    private readonly StackerContent _stackerContent = new();
    private readonly OtherOptionsContent _otherOptionsContent = new();

    public ConfigPageViewModel()
    {
        _activeContent = _managerContent;
    }

    partial void OnSearchStringChanged(string value)
    {
        Manager.SearchString = value;
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalLabel));
    }

    partial void OnSelectedConfigChanged(ConfigViewModel value)
    {
        // A null push (grid re-render clearing SelectedItem) must not wipe the
        // current config — Other Options / Stacker rely on it staying set.
        if (value == null) return;

        Manager.CurrentConfig = value;
        Manager.HoveredConfig = value?.Config;
        OnPropertyChanged(nameof(HoveredConfig));
        OnPropertyChanged(nameof(CurrentConfigName));
        {
            LoliScript = value.Config.Script;
            _stackerContent.StackerVM = new StackerViewModel(value);
            _otherOptionsContent.ConfigSettings = value.Config.Settings;
            _otherOptionsContent.Base64Image = value.Config.Settings.Base64Image;

            // Preserve debugger state across config switches
            if (OB.Stacker != null)
            {
                _stackerContent.StackerVM.TestData = OB.Stacker.TestData;
                _stackerContent.StackerVM.TestProxy = OB.Stacker.TestProxy;
                _stackerContent.StackerVM.ProxyType = OB.Stacker.ProxyType;
            }
            OB.Stacker = _stackerContent.StackerVM;
        }
    }

    [RelayCommand]
    private void SwitchToManager()
    {
        ActiveTab = ConfigTab.Manager;
        ActiveContent = _managerContent;
    }

    [RelayCommand]
    private void SwitchToStacker()
    {
        var cfg = SelectedConfig ?? Manager.CurrentConfig;
        if (cfg == null) return;
        SelectedConfig = cfg;
        _stackerContent.StackerVM = new StackerViewModel(cfg);
        ActiveTab = ConfigTab.Stacker;
        ActiveContent = _stackerContent;
    }

    [RelayCommand]
    private void SwitchToOtherOptions()
    {
        var cfg = SelectedConfig ?? Manager.CurrentConfig;
        if (cfg == null) return;
        SelectedConfig = cfg;
        _otherOptionsContent.ConfigSettings = cfg.Config.Settings;
        ActiveTab = ConfigTab.OtherOptions;
        ActiveContent = _otherOptionsContent;
    }

    [RelayCommand]
    private void Rescan()
    {
        Manager.Rescan();
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalLabel));
    }

    [RelayCommand]
    private void DeleteConfig()
    {
        if (SelectedConfig != null)
        {
            Manager.Remove(SelectedConfig);
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(TotalLabel));
        }
    }

    public void DeleteConfigs(IEnumerable<ConfigViewModel> configs)
    {
        Manager.Remove(configs);
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalLabel));
    }
}
