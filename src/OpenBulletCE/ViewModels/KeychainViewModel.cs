using RuriLib;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBulletCE.ViewModels;

public class KeychainViewModel : ViewModelBase
{
    private Random rand = new Random(3);

    private int _id;
    public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }

    public KeyChain Keychain { get; set; }
    public ObservableCollection<KeyViewModel> KeyList { get; set; } = new();

    public KeyChain.KeychainType Type
    {
        get => Keychain.Type;
        set { Keychain.Type = value; OnPropertyChanged(); OnPropertyChanged(nameof(KeychainColor)); OnPropertyChanged(nameof(CustomVisibility)); }
    }
    public bool TypeInitialized { get; set; }

    public KeyChain.KeychainMode Mode
    {
        get => Keychain.Mode;
        set { Keychain.Mode = value; OnPropertyChanged(); }
    }
    public bool ModeInitialized { get; set; }

    public string CustomType
    {
        get => Keychain.CustomType;
        set { Keychain.CustomType = value; OnPropertyChanged(); OnPropertyChanged(nameof(KeychainColor)); }
    }
    public bool CustomVisibility => Type == KeyChain.KeychainType.Custom;
    public bool CustomTypeInitialized { get; set; }

    public string KeychainColor
    {
        get
        {
            return Type switch
            {
                KeyChain.KeychainType.Success => "#006600",
                KeyChain.KeychainType.Failure => "#cc0000",
                KeyChain.KeychainType.Custom =>
                    $"#{OB.Settings.Environment.GetCustomKeychain(CustomType).Color.A:X2}" +
                    $"{OB.Settings.Environment.GetCustomKeychain(CustomType).Color.R:X2}" +
                    $"{OB.Settings.Environment.GetCustomKeychain(CustomType).Color.G:X2}" +
                    $"{OB.Settings.Environment.GetCustomKeychain(CustomType).Color.B:X2}",
                KeyChain.KeychainType.Ban => "#660066",
                KeyChain.KeychainType.Retry => "#cc9900",
                _ => "#000000"
            };
        }
    }

    public KeyViewModel GetKeyById(int id)
        => KeyList.First(x => x.Id.KeyId == id);

    public void RemoveKeyById(int id)
    {
        Keychain.Keys.Remove(GetKeyById(id).Key);
        KeyList.Remove(GetKeyById(id));
    }

    public void AddKey()
    {
        var key = new Key();
        Keychain.Keys.Add(key);
        KeyList.Add(new KeyViewModel(key, rand.Next(), Id));
    }

    public KeychainViewModel(KeyChain keychain, int id)
    {
        Keychain = keychain;
        Id = id;
        foreach (Key key in keychain.Keys)
        {
            KeyList.Add(new KeyViewModel(key, rand.Next(), Id));
        }
    }
}
