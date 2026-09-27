using RuriLib.Interfaces;

namespace OpenBulletCE;

public struct OpenBulletApp : IApplication
{
    public IRunnerManager RunnerManager { get; set; }
    public IProxyManager ProxyManager { get; set; }
    public IProxyChecker ProxyChecker { get; set; }
    public IWordlistManager WordlistManager { get; set; }
    public ICookieManager CookieManager { get; set; }
    public IConfigManager ConfigManager { get; set; }
    public IHitsDB HitsDB { get; set; }
    public IAlerter Alerter { get; set; }
    public ILogger Logger { get; set; }
    public ISettings Settings { get; set; }
}
