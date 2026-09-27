using Extreme.Net;
using LiteDB;
using Newtonsoft.Json.Linq;
using OpenBulletCE.Repositories;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.Models.Stats;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenBulletCE.ViewModels;

public class ProxyManagerViewModel : ViewModelBase, IProxyManager, IProxyChecker
{
    private LiteDBRepository<CProxy> _repo;

    private ObservableCollection<CProxy> _proxiesCollection = new();
    public ObservableCollection<CProxy> ProxiesCollection
    {
        get => _proxiesCollection;
        private set
        {
            _proxiesCollection = value;
            OnPropertyChanged();
            UpdateProperties();
        }
    }

    public IEnumerable<CProxy> Proxies => ProxiesCollection;

    public ProxyManagerViewModel()
    {
        _repo = new LiteDBRepository<CProxy>(OB.dataBaseFile, "proxies");
        ProxiesCollection = new ObservableCollection<CProxy>();
    }

    #region Statistics
    private int _total;
    public int Total { get => _total; set { _total = value; OnPropertyChanged(); } }

    private int _tested;
    public int Tested { get => _tested; set { _tested = value; OnPropertyChanged(); } }

    private int _http;
    public int Http { get => _http; set { _http = value; OnPropertyChanged(); } }

    private int _socks4;
    public int Socks4 { get => _socks4; set { _socks4 = value; OnPropertyChanged(); } }

    private int _socks4a;
    public int Socks4a { get => _socks4a; set { _socks4a = value; OnPropertyChanged(); } }

    private int _socks5;
    public int Socks5 { get => _socks5; set { _socks5 = value; OnPropertyChanged(); } }

    private int _chain;
    public int Chain { get => _chain; set { _chain = value; OnPropertyChanged(); } }

    private int _working;
    public int Working { get => _working; set { _working = value; OnPropertyChanged(); } }

    private int _notWorking;
    public int NotWorking { get => _notWorking; set { _notWorking = value; OnPropertyChanged(); } }

    public void UpdateProperties()
    {
        Total = ProxiesCollection.Count;
        Tested = ProxiesCollection.Count(x => x.Working != ProxyWorking.UNTESTED);
        Http = ProxiesCollection.Count(x => x.Type == ProxyType.Http);
        Socks4 = ProxiesCollection.Count(x => x.Type == ProxyType.Socks4);
        Socks4a = ProxiesCollection.Count(x => x.Type == ProxyType.Socks4a);
        Socks5 = ProxiesCollection.Count(x => x.Type == ProxyType.Socks5);
        Chain = ProxiesCollection.Count(x => x.Type == ProxyType.Chain);
        Working = ProxiesCollection.Count(x => x.Working == ProxyWorking.YES);
        NotWorking = ProxiesCollection.Count(x => x.Working == ProxyWorking.NO);
    }

    public ProxyManagerStats Stats => new ProxyManagerStats(Total, Tested, Working, Http, Socks4, Socks4a, Socks5);
    #endregion

    #region Checker
    private int _botsAmount = 1;
    public int BotsAmount { get => _botsAmount; set { _botsAmount = value; OnPropertyChanged(); } }

    public string TestSite
    {
        get => OB.Settings.ProxyManagerSettings.ActiveProxySiteUrl;
        set { OB.Settings.ProxyManagerSettings.ActiveProxySiteUrl = value; OnPropertyChanged(); }
    }

    public string SuccessKey
    {
        get => OB.Settings.ProxyManagerSettings.ActiveProxyKey;
        set { OB.Settings.ProxyManagerSettings.ActiveProxyKey = value; OnPropertyChanged(); }
    }

    private bool _onlyUntested = true;
    public bool OnlyUntested { get => _onlyUntested; set { _onlyUntested = value; OnPropertyChanged(); } }

    private int _timeout = 2;
    public int Timeout { get => _timeout; set { _timeout = value; OnPropertyChanged(); } }

    public static readonly int maximumBots = 200;

    public async Task CheckAllAsync(IEnumerable<CProxy> proxies, CancellationToken cancellationToken,
        Action<CheckResult<ProxyResult>>? onResult = null, IProgress<float>? progress = null)
    {
        using (var ss = new SemaphoreSlim(BotsAmount, BotsAmount))
        {
            var total = proxies.Count();
            var current = 0;

            var tasks = proxies.Select(async proxy =>
            {
                await ss.WaitAsync();

                CheckResult<ProxyResult> checkResult = new CheckResult<ProxyResult>();
                ProxyResult proxyResult = new ProxyResult();
                proxyResult.proxy = proxy;

                try
                {
                    proxyResult = await CheckProxy(proxy);
                    checkResult = new CheckResult<ProxyResult>(true, proxyResult);
                }
                catch (Exception ex)
                {
                    checkResult = new CheckResult<ProxyResult>(false, proxyResult, ex.Message);
                }
                finally
                {
                    onResult?.Invoke(checkResult);
                    progress?.Report((float)++current / total);
                    ss.Release();
                }
            });

            await Task.WhenAny(Task.WhenAll(tasks), AsTask(cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public async Task<ProxyResult> CheckAsync(CProxy proxy, CancellationToken cancellationToken)
    {
        var task = CheckProxy(proxy);
        await Task.WhenAny(task, AsTask(cancellationToken));

        cancellationToken.ThrowIfCancellationRequested();
        return task.Result;
    }

    private async Task<ProxyResult> CheckProxy(CProxy proxy)
    {
        ProxyResult result = new ProxyResult();
        result.proxy = proxy;

        var sw = new Stopwatch();
        sw.Start();

        result.working = await CheckWorking(proxy);

        sw.Stop();
        result.ping = (int)sw.ElapsedMilliseconds;

        try
        {
            result.country = await CheckCountry(proxy);
        }
        catch { }

        return result;
    }

    private async Task<bool> CheckWorking(CProxy proxy)
    {
        var timeout = Timeout * 1000;
        using (var request = new HttpRequest())
        {
            request.Proxy = proxy.GetClient();
            request.Proxy.ConnectTimeout = timeout;
            request.Proxy.ReadWriteTimeout = timeout;
            request.ConnectTimeout = timeout;
            request.KeepAliveTimeout = timeout;
            request.ReadWriteTimeout = timeout;
            var response = await request.GetAsync(TestSite);
            var source = response.ToString();

            return source.Contains(SuccessKey);
        }
    }

    private async Task<string> CheckCountry(CProxy proxy)
    {
        using (var request = new HttpRequest())
        {
            request.ConnectTimeout = Timeout;
            var response = await request.GetAsync("http://ip-api.com/json/" + proxy.Host);
            var json = JObject.Parse(response.ToString());
            var status = json.Value<string>("status");

            return status == "success" ? json.Value<string>("country") : "Unknown";
        }
    }

    public static Task AsTask(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<object>();
        cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);
        return tcs.Task;
    }
    #endregion

    #region CRUD Operations
    public void Add(CProxy proxy)
    {
        ProxiesCollection.Add(proxy);
        UpdateProperties();
        _repo.Add(proxy);
    }

    public void AddRange(IEnumerable<CProxy> proxies)
    {
        foreach (var p in proxies)
            ProxiesCollection.Add(p);

        UpdateProperties();
        _repo.Add(proxies);
    }

    public void RefreshList()
    {
        ProxiesCollection = new ObservableCollection<CProxy>(_repo.Get());
    }

    public void Update(CProxy proxy)
    {
        _repo.Update(proxy);
    }

    public void Remove(CProxy proxy)
    {
        ProxiesCollection.Remove(proxy);
        UpdateProperties();
        _repo.Remove(proxy);
    }

    public void Remove(IEnumerable<CProxy> proxies)
    {
        var toRemove = proxies.ToArray();
        foreach (var proxy in toRemove)
            ProxiesCollection.Remove(proxy);

        UpdateProperties();
        _repo.Remove(toRemove);
    }

    public void RemoveAll()
    {
        ProxiesCollection.Clear();
        UpdateProperties();
        _repo.RemoveAll();
    }
    #endregion

    #region Delete methods
    public void RemoveNotWorking()
        => Remove(Proxies.Where(p => p.Working == ProxyWorking.NO).ToList());

    public void RemoveDuplicates()
    {
        var duplicates = Proxies
            .GroupBy(p => p.Proxy)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.OrderBy(p => p.Proxy).Reverse().Skip(1))
            .ToList();

        Remove(duplicates);
    }

    public void RemoveUntested()
        => Remove(Proxies.Where(p => p.Working == ProxyWorking.UNTESTED).ToList());
    #endregion
}
