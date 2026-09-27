using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenBulletCE.ViewModels.Pages;

public partial class ProxyPageViewModel : ViewModelBase
{
    public ProxyManagerViewModel Manager => OB.ProxyManager;

    // Expose the real collection
    public ObservableCollection<CProxy> Proxies => Manager.ProxiesCollection;

    public System.Collections.Generic.IEnumerable<string> TestSiteUrls => OB.Settings.ProxyManagerSettings.ProxySiteUrls;
    public System.Collections.Generic.IEnumerable<string> SuccessKeys => OB.Settings.ProxyManagerSettings.ProxyKeys;

    // Stats
    public int TotalCount => Manager.Total;
    public int AliveCount => Manager.Working;
    public int DeadCount => Manager.NotWorking;
    public int UncheckedCount => Manager.Total - Manager.Tested;
    public int HttpCount => Manager.Http;
    public int Socks4Count => Manager.Socks4;
    public int Socks4aCount => Manager.Socks4a;
    public int Socks5Count => Manager.Socks5;

    [ObservableProperty]
    private string _newProxyText = "";

    [ObservableProperty]
    private string _searchString = "";

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private double _checkProgress;

    private CancellationTokenSource _checkCts;

    public ProxyPageViewModel()
    {
        Manager.RefreshList();
        Manager.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(Manager.Total) or nameof(Manager.Working)
                or nameof(Manager.NotWorking) or nameof(Manager.Tested))
            {
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(AliveCount));
                OnPropertyChanged(nameof(DeadCount));
                OnPropertyChanged(nameof(UncheckedCount));
            }
        };
    }

    [RelayCommand]
    private void AddProxies()
    {
        if (string.IsNullOrWhiteSpace(NewProxyText)) return;

        var lines = NewProxyText.Split('\n', '\r')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        var proxies = lines.Select(line =>
        {
            try { return new CProxy().Parse(line); }
            catch { return null; }
        }).Where(p => p != null).ToList();

        Manager.AddRange(proxies);
        NewProxyText = "";
        RefreshStats();
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        // Views pass selected items
    }

    [RelayCommand]
    private void RemoveNotWorking()
    {
        Manager.RemoveNotWorking();
        RefreshStats();
    }

    [RelayCommand]
    private void RemoveDuplicates()
    {
        Manager.RemoveDuplicates();
        RefreshStats();
    }

    [RelayCommand]
    private void RemoveUntested()
    {
        Manager.RemoveUntested();
        RefreshStats();
    }

    [RelayCommand]
    private async Task CheckProxies()
    {
        if (IsChecking) return;

        IsChecking = true;
        _checkCts = new CancellationTokenSource();

        var toCheck = Manager.OnlyUntested
            ? Manager.Proxies.Where(p => p.Working == ProxyWorking.UNTESTED).ToList()
            : Manager.Proxies.ToList();

        var progress = new Progress<float>(p => CheckProgress = p * 100);
        var checkedSoFar = 0;

        try
        {
            await Manager.CheckAllAsync(toCheck, _checkCts.Token, result =>
            {
                var proxy = result.result.proxy;
                if (proxy != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        proxy.Working = result.result.working ? ProxyWorking.YES : ProxyWorking.NO;
                        proxy.Ping = result.result.ping;
                        proxy.Country = result.result.country ?? "";
                        proxy.LastChecked = DateTime.Now;
                    });
                    // Persist so the tested state survives page reloads/restarts
                    try { Manager.Update(proxy); } catch { }
                    // Live stats without an O(n) recount per proxy
                    if (++checkedSoFar % 25 == 0)
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => Manager.UpdateProperties());
                }
            }, progress);
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsChecking = false;
            CheckProgress = 0;
            RefreshStats();
        }
    }

    [RelayCommand]
    private void StopCheck()
    {
        _checkCts?.Cancel();
    }

    [RelayCommand]
    private void ClearAll()
    {
        Manager.RemoveAll();
        RefreshStats();
    }

    private void RefreshStats()
    {
        Manager.UpdateProperties();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(AliveCount));
        OnPropertyChanged(nameof(DeadCount));
        OnPropertyChanged(nameof(UncheckedCount));
        OnPropertyChanged(nameof(HttpCount));
        OnPropertyChanged(nameof(Socks4Count));
        OnPropertyChanged(nameof(Socks4aCount));
        OnPropertyChanged(nameof(Socks5Count));
    }
}
