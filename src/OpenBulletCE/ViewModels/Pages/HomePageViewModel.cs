using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenBulletCE.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    public string Welcome => "Welcome to OpenBullet Cookie Edition";

    public string Version => $"v{OB.Version}";

    public int ConfigsCount => OB.ConfigManager?.Configs.Count() ?? 0;
    public int WordlistsCount => OB.WordlistManager?.Wordlists.Count() ?? 0;
    public int ProxiesCount => OB.ProxyManager?.Proxies.Count() ?? 0;
    public int HitsCount => OB.HitsDB?.Hits.Count() ?? 0;
    public int CookiesCount => OB.CookieManager?.Cookies.Count() ?? 0;

    public void Refresh()
    {
        OnPropertyChanged(nameof(ConfigsCount));
        OnPropertyChanged(nameof(WordlistsCount));
        OnPropertyChanged(nameof(ProxiesCount));
        OnPropertyChanged(nameof(HitsCount));
    }
}
