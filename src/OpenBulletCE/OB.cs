using OpenBulletCE.ViewModels;
using PluginFramework;
using RuriLib;
using RuriLib.Interfaces;
using RuriLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenBulletCE;

public static class OB
{
    public static IApplication App => new OpenBulletApp()
    {
        RunnerManager = RunnerManager,
        ProxyManager = ProxyManager,
        ProxyChecker = ProxyManager,
        WordlistManager = WordlistManager,
        CookieManager = CookieManager,
        ConfigManager = ConfigManager,
        HitsDB = HitsDB,
        Settings = Settings,
        Logger = Logger,
        Alerter = Alerter
    };

    public static string Version => "1.8.9";

    // Block Mappings (including Plugins): BlockType -> PageType -> BlockColor
    public static List<(Type, Type, Color)> BlockMappings = new List<(Type, Type, Color)>();

    // Block Plugins
    public static List<IBlockPlugin> BlockPlugins;
    public static IEnumerable<BlockBase> BlockPluginsAsBase => BlockPlugins.Cast<BlockBase>();

    // ViewModels
    public static RunnerManagerViewModel RunnerManager { get; set; }
    public static ProxyManagerViewModel ProxyManager { get; set; }
    public static WordlistManagerViewModel WordlistManager { get; set; }
    public static CookieManagerViewModel CookieManager { get; set; }
    public static ConfigManagerViewModel ConfigManager { get; set; }
    public static StackerViewModel Stacker { get; set; }
    public static HitsDBViewModel HitsDB { get; set; }
    public static Alerter Alerter { get; set; } = new Alerter();
    public static LoggerViewModel Logger { get; set; } = new LoggerViewModel();
    public static GlobalSettings Settings { get; set; } = new GlobalSettings();
    public static OBSettingsViewModel OBSettings { get; set; }

    // Constant file paths
    public static readonly string dataBaseFile = @"DB/OpenBullet.db";
    public static readonly string dataBaseBackupFile = @"DB/OpenBullet-BackupCopy.db";
    public static readonly string obSettingsFile = @"Settings/OBSettings.json";
    public static readonly string rlSettingsFile = @"Settings/RLSettings.json";
    public static readonly string proxyManagerSettingsFile = @"Settings/ProxyManagerSettings.json";
    public static readonly string envFile = @"Settings/Environment.ini";
    public static readonly string licenseFile = @"Settings/License.txt";
    public static readonly string logFile = @"Log.txt";
    public static readonly string configFolder = @"Configs";
    public static readonly string pluginsFolder = @"Plugins";
    public static readonly string defaultProxySiteUrl = "https://google.com";
    public static readonly string defaultProxyKey = "title>Google";
}
