using OpenBulletCE.Models;
using OpenBulletCE.Repositories;
using PluginFramework;
using RuriLib;
using RuriLib.Functions.Formats;
using RuriLib.Interfaces;
using RuriLib.LS;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenBulletCE.ViewModels;

public class ConfigManagerViewModel : ViewModelBase, IConfigManager
{
    private ConfigRepository _diskRepo;

    private ObservableCollection<ConfigViewModel> _configsCollection = new();
    private ObservableCollection<ConfigViewModel> _filteredConfigs = new();

    // The view binds to this — pre-filtered
    public ObservableCollection<ConfigViewModel> ConfigsCollection => _filteredConfigs;

    private List<ConfigViewModel> _allConfigs = new();

    public IEnumerable<ConfigViewModel> Configs => _allConfigs;

    public int Total => _filteredConfigs.Count;

    public int SavedHash { get; set; } = 0;

    private Config _hoveredConfig;
    public Config HoveredConfig
    {
        get => _hoveredConfig;
        set { _hoveredConfig = value; OnPropertyChanged(); }
    }

    private ConfigViewModel _currentConfig;
    public ConfigViewModel CurrentConfig
    {
        get => _currentConfig;
        set
        {
            _currentConfig = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentConfigName));
        }
    }

    public string CurrentConfigName => CurrentConfig == null ? "None" : CurrentConfig.Name;

    public ConfigManagerViewModel()
    {
        _diskRepo = new ConfigRepository(OB.configFolder);
        Rescan();
    }

    public IEnumerable<string> GetRequiredPlugins(ConfigViewModel config)
    {
        return new LoliScript(config.Config.Script)
            .ToBlocks()
            .OnlyPlugins()
            .Cast<IBlockPlugin>()
            .Select(p => p.Name)
            .Distinct();
    }

    #region Filters
    private string _searchString = "";
    public string SearchString
    {
        get => _searchString;
        set
        {
            _searchString = value;
            OnPropertyChanged();
            ApplyFilter();
            OnPropertyChanged(nameof(Total));
        }
    }

    private void ApplyFilter()
    {
        _filteredConfigs.Clear();
        foreach (var c in _allConfigs.Where(PassesFilter))
            _filteredConfigs.Add(c);
    }

    private bool PassesFilter(ConfigViewModel item)
        => item.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
    #endregion

    #region Get from disk
    public List<ConfigViewModel> GetConfigsFromDisk(bool sort = false, bool reverse = false)
    {
        var configs = _diskRepo.Get().ToList();

        if (sort)
        {
            configs.Sort((m1, m2) => m1.Config.Settings.LastModified.CompareTo(m2.Config.Settings.LastModified));
            if (reverse) configs.Reverse();
        }
        return configs;
    }
    #endregion

    #region Get from sources
    public IEnumerable<ConfigViewModel> GetConfigsFromSources()
    {
        var configs = new List<ConfigViewModel>();

        foreach (var source in OB.OBSettings.Sources.Sources)
        {
            try
            {
                configs.AddRange(PullSource(source));
            }
            catch (Exception ex)
            {
                OB.Logger.LogError(Components.ConfigManager, $"Error with API {source.ApiUrl}\r\nReason: {ex.Message}", true);
            }
        }

        return configs;
    }

    public IEnumerable<ConfigViewModel> PullSource(Source source)
    {
        var configs = new List<ConfigViewModel>();

        using (var client = new HttpClient())
        {
            switch (source.Auth)
            {
                case Source.AuthMode.ApiKey:
                    client.DefaultRequestHeaders.Add("Authorization", source.ApiKey);
                    break;
                case Source.AuthMode.UserPass:
                    var header = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{source.Username}:{source.Password}"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {header}");
                    break;
            }

            var response = client.GetAsync(source.ApiUrl).Result;

            if (response.Headers.TryGetValues("Result", out var vals) && vals.FirstOrDefault() == "Error")
            {
                var body = response.Content.ReadAsStringAsync().Result;
                throw new Exception($"The server says: {body}");
            }

            var file = response.Content.ReadAsByteArrayAsync().Result;

            try
            {
                using (var zip = new ZipArchive(new MemoryStream(file), ZipArchiveMode.Read))
                {
                    foreach (var entry in zip.Entries)
                    {
                        var subCategory = Path.GetDirectoryName(entry.FullName)?.Replace("\\", " - ") ?? "";
                        var category = subCategory == string.Empty ? "Remote" : $"Remote - {subCategory}";
                        using (var stream = entry.Open())
                        using (TextReader tr = new StreamReader(stream))
                        {
                            var text = tr.ReadToEnd();
                            var cfg = IOManager.DeserializeConfig(text);
                            configs.Add(new ConfigViewModel("", category, cfg, true));
                        }
                    }
                }
            }
            catch { }
        }

        return configs;
    }
    #endregion

    #region CRUD Operations
    public void CreateConfig(string name, RuriLib.Enums.ConfigType type, string category, string author)
    {
        var settings = new ConfigSettings
        {
            Name = name,
            Author = author,
            Type = type
        };

        var newConfig = new ConfigViewModel(name, category, new Config(settings, string.Empty));
        Add(newConfig);
        CurrentConfig = newConfig;
        OB.Logger.LogInfo(Components.ConfigManager, $"Created config {name}");
    }

    public void Add(ConfigViewModel config)
    {
        _diskRepo.Add(config);
        _allConfigs.Add(config);
        ApplyFilter();
        OnPropertyChanged(nameof(Total));
    }

    public void Rescan()
    {
        _allConfigs = GetConfigsFromDisk(true, true)
            .Concat(GetConfigsFromSources())
            .ToList();

        ApplyFilter();
        OnPropertyChanged(nameof(Total));
    }

    public void Update(ConfigViewModel config)
    {
        _diskRepo.Update(config);
    }

    public void SaveCurrent()
    {
        Update(CurrentConfig);
    }

    public void Remove(ConfigViewModel config)
    {
        _diskRepo.Remove(config);
        _allConfigs.Remove(config);
        _filteredConfigs.Remove(config);
        OnPropertyChanged(nameof(Total));
    }

    public void Remove(IEnumerable<ConfigViewModel> configs)
    {
        var toRemove = configs.ToArray();
        foreach (var config in toRemove)
        {
            _allConfigs.Remove(config);
            _filteredConfigs.Remove(config);
        }
        _diskRepo.Remove(toRemove);
        OnPropertyChanged(nameof(Total));
    }
    #endregion
}
