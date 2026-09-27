using Extreme.Net;
using PluginFramework;
using RuriLib;
using RuriLib.LS;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBulletCE.ViewModels;

public enum StackerView
{
    LoliScript,
    Blocks
}

public class StackerViewModel : ViewModelBase
{
    Random rand = new Random();

    public StackerView View { get; set; } = StackerView.LoliScript;
    public BotData BotData { get; set; }
    public LoliScript LS { get; set; }

    private bool _controlsEnabled = true;
    public bool ControlsEnabled { get => _controlsEnabled; set { _controlsEnabled = value; OnPropertyChanged(); } }

    public ConfigViewModel Config { get; set; }

    public ObservableCollection<StackerBlockViewModel> Stack { get; set; } = new();
    public List<StackerBlockViewModel> SelectedBlocks => Stack.Where(b => b.Selected).ToList();

    public StackerBlockViewModel CurrentBlock { get; set; }
    public int CurrentBlockIndex => Stack.IndexOf(CurrentBlock);

    public void DeselectAll() { foreach (var b in Stack) b.Selected = false; }
    public void SelectAll() { foreach (var b in Stack) b.Selected = true; }

    public BlockBase LastDeletedBlock { get; set; }
    public int LastDeletedIndex { get; set; }

    public List<BlockBase> GetList()
    {
        ConvertKeychains();
        ConvertPlugins();
        return Stack.Select(x => x.Block).ToList();
    }

    public void ConvertKeychains()
    {
        foreach (StackerBlockViewModel stackerBlock in Stack)
        {
            var block = stackerBlock.Block;
            if (block is BlockKeycheck kcblock && stackerBlock.KeychainList != null)
            {
                kcblock.KeyChains = stackerBlock.KeychainList.Select(k => k.Keychain).ToList();
            }
        }
    }

    public void ConvertPlugins()
    {
        foreach (StackerBlockViewModel stackerBlock in Stack.Where(sb => sb.Block.IsPlugin()))
        {
            stackerBlock.SetPluginPropertyValues?.Invoke();
        }
    }

    public StackerBlockViewModel GetBlockById(int id)
        => Stack.First(x => x.Id == id);

    public void AddBlock(BlockBase block, int index = -1)
    {
        if (index < 0 || index > Stack.Count) index = Stack.Count;
        Stack.Insert(index, new StackerBlockViewModel(block, rand));
        UpdateHeights();
    }

    public void ClearBlocks()
    {
        Stack.Clear();
        UpdateHeights();
    }

    public void UpdateHeights()
    {
        foreach (var s in Stack)
            s.UpdateHeight(Clamp(600 / Math.Max(Stack.Count, 1), 26, 38));
    }

    public void MoveBlockUp(StackerBlockViewModel block)
    {
        var oldIndex = Stack.IndexOf(block);
        if (oldIndex != 0)
            Stack.Move(oldIndex, oldIndex - 1);
    }

    public void MoveBlockDown(StackerBlockViewModel block)
    {
        var oldIndex = Stack.IndexOf(block);
        if (oldIndex != Stack.Count - 1)
            Stack.Move(oldIndex, oldIndex + 1);
    }

    public int Clamp(int a, int min, int max)
    {
        if (a < min) return min;
        if (a > max) return max;
        return a;
    }

    // DEBUGGER CONTROLS
    private ProxyType _proxyType = ProxyType.Http;
    public ProxyType ProxyType { get => _proxyType; set { _proxyType = value; OnPropertyChanged(); } }

    private string _testData = "";
    public string TestData { get => _testData; set { _testData = value; OnPropertyChanged(); } }

    private string _testDataType = "";
    public string TestDataType { get => _testDataType; set { _testDataType = value; OnPropertyChanged(); } }

    private string _testProxy = "";
    public string TestProxy { get => _testProxy; set { _testProxy = value; OnPropertyChanged(); } }

    private bool _useProxy;
    public bool UseProxy
    {
        get => _useProxy;
        set { _useProxy = value; OnPropertyChanged(); OnPropertyChanged(nameof(UseProxyString)); OnPropertyChanged(nameof(UseProxyColor)); }
    }
    public string UseProxyString => UseProxy ? "ON" : "OFF";
    public string UseProxyColor => UseProxy ? OB.OBSettings.Themes.ForegroundGood : OB.OBSettings.Themes.ForegroundBad;

    private bool _sbs;
    public bool SBS { get => _sbs; set { _sbs = value; OnPropertyChanged(); } }

    private bool _sbsClear;
    public bool SBSClear { get => _sbsClear; set { _sbsClear = value; OnPropertyChanged(); } }

    private bool _sbsEnabled;
    public bool SBSEnabled { get => _sbsEnabled; set { _sbsEnabled = value; OnPropertyChanged(); } }

    // Search
    private string _searchString = "";
    public string SearchString { get => _searchString; set { _searchString = value; OnPropertyChanged(); OnPropertyChanged(nameof(SearchProgress)); } }

    private List<int> _indexes = new();
    public List<int> Indexes
    {
        get => _indexes;
        set { _indexes = value; OnPropertyChanged(nameof(TotalSearchMatches)); OnPropertyChanged(nameof(CurrentSearchMatch)); }
    }
    public int TotalSearchMatches => Indexes.Count;
    public string SearchProgress => $"{CurrentSearchMatch + 1}/{TotalSearchMatches}";

    public void UpdateTotalSearchMatches()
        => OnPropertyChanged(nameof(TotalSearchMatches));

    private int _currentSearchMatch;
    public int CurrentSearchMatch { get => _currentSearchMatch; set { _currentSearchMatch = value; OnPropertyChanged(); } }

    public StackerViewModel(ConfigViewModel config)
    {
        Config = config;
    }
}
