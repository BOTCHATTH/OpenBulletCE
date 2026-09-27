using Avalonia.Controls;
using PluginFramework;
using RuriLib;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenBulletCE.ViewModels;

public class StackerBlockViewModel : ViewModelBase
{
    private int _id;
    public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }

    private bool _selected;
    public bool Selected { get => _selected; set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(BorderColor)); } }
    public string BorderColor => _selected ? "#FFFFFF" : "#FF000000";

    public string Color => Block.Disabled ? "#808080" : $"#{GetBlockColor().A:X2}{GetBlockColor().R:X2}{GetBlockColor().G:X2}{GetBlockColor().B:X2}";

    public string Foreground => Block.IsSelenium ? "#FFFFFF" : "#000000";

    private int _height;
    public int Height { get => _height; set { _height = value; OnPropertyChanged(); OnPropertyChanged(nameof(FontSize)); } }
    public int FontSize => Math.Clamp(Height / 2, 13, 18);

    private BlockBase _block;
    public BlockBase Block { get => _block; set { _block = value; OnPropertyChanged(); } }

    // Replaces the WPF Page reference — the ViewLocator resolves this to a UserControl
    private UserControl _page;
    public UserControl Page { get => _page; set { _page = value; OnPropertyChanged(); } }

    // Callback for plugin property sync (replaces direct Page cast)
    public Action SetPluginPropertyValues { get; set; }

    // Callback for keychain sync (replaces direct Page cast)
    public List<KeychainViewModel> KeychainList { get; set; }

    public void Disable()
    {
        if (Block.GetType() == typeof(BlockLSCode)) return;
        Block.Disabled = !Block.Disabled;
        OnPropertyChanged(nameof(Color));
    }

    public void UpdateHeight(int height)
    {
        Height = height;
        OnPropertyChanged(nameof(Height));
        OnPropertyChanged(nameof(FontSize));
    }

    public StackerBlockViewModel(BlockBase block, Random rand)
    {
        Id = rand.Next();
        Block = block;
        OnPropertyChanged(nameof(Block.Label));
        OnPropertyChanged(nameof(Color));
    }

    public Color GetBlockColor()
        => OB.BlockMappings.First(m => m.Item1 == Block.GetType()).Item3;
}
