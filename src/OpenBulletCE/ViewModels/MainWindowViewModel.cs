using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenBulletCE.ViewModels.Pages;
using System.Collections.Generic;

namespace OpenBulletCE.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private string _currentPageName = "Home";

    [ObservableProperty]
    private bool _isLogOpen = false;

    public LogViewModel Log { get; } = new();

    private readonly Dictionary<string, ViewModelBase> _pages = new();

    public MainWindowViewModel()
    {
        _pages["Home"] = new HomePageViewModel();
        _pages["Runner"] = new RunnerPageViewModel();
        _pages["Proxies"] = new ProxyPageViewModel();
        _pages["Wordlists"] = new WordlistPageViewModel();
        _pages["Cookies"] = new CookiePageViewModel();
        _pages["Configs"] = new ConfigPageViewModel();
        _pages["HitsDB"] = new HitsDBPageViewModel();
        _pages["Tools"] = new ToolsPageViewModel();
        _pages["Plugins"] = new PluginsPageViewModel();
        _pages["MCP"] = new McpPageViewModel();
        _pages["Settings"] = new SettingsPageViewModel();
        _pages["About"] = new AboutPageViewModel();

        _currentPage = _pages["Home"];
    }

    [RelayCommand]
    private void Navigate(string pageName)
    {
        if (_pages.TryGetValue(pageName, out var page))
        {
            if (page is HomePageViewModel h) h.Refresh();
            CurrentPage = page;
            CurrentPageName = pageName;
        }
    }

    [RelayCommand]
    private void ToggleLog()
    {
        IsLogOpen = !IsLogOpen;
    }
}
