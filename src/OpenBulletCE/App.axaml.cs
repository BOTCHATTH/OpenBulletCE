using Avalonia;
using Avalonia.Controls;
using AvaloniaWebView;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenBulletCE.Models;
using OpenBulletCE.Plugins;
using OpenBulletCE.Services;
using OpenBulletCE.ViewModels;
using OpenBulletCE.Views;
using RuriLib;
using RuriLib.LS;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.IO;
using System.Linq;

namespace OpenBulletCE;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void RegisterServices()
    {
        base.RegisterServices();
        AvaloniaWebViewBuilder.Initialize(default);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                OnUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                OnUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");

            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            desktop.ShutdownRequested += (_, _) => SaveAllSettings();

            // Wire up dialog service
            Alerter.Dialogs = new DialogService(mainWindow);

            InitializeOB();

            mainWindow.DataContext = new MainWindowViewModel();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnUnhandledException(Exception ex, string @event)
    {
        try
        {
            File.AppendAllText(OB.logFile, $"[FATAL][{@event}] UNHANDLED EXCEPTION{Environment.NewLine}{ex}");
        }
        catch { }
    }

    private void InitializeOB()
    {
        // Anchor every relative path (Settings/, DB/, Configs/, ...) to the exe directory
        // so the app reads/writes the same files regardless of launch context
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        // Clean or create log file
        try { File.WriteAllText(OB.logFile, ""); } catch { }

        // Make sure all folders exist
        var folders = new[] { "Captchas", "ChromeExtensions", "Configs", "DB", "Hits", "Plugins", "Screenshots", "Settings", "Sounds", "Wordlists", "Cookies" };
        foreach (var folder in folders.Select(f => Path.Combine(AppContext.BaseDirectory, f)))
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        // Initialize Environment Settings
        try
        {
            OB.Settings.Environment = IOManager.ParseEnvironmentSettings(OB.envFile);
        }
        catch
        {
            OB.Logger.LogError(Components.Main,
                "Could not find / parse the Environment Settings file. Please fix the issue and try again.", true);
            Environment.Exit(0);
            return;
        }

        if (OB.Settings.Environment.WordlistTypes.Count == 0 || OB.Settings.Environment.CustomKeychains.Count == 0)
        {
            OB.Logger.LogError(Components.Main,
                "At least one WordlistType and one CustomKeychain must be defined in the Environment Settings file.", true);
            Environment.Exit(0);
            return;
        }

        // Initialize Settings
        OB.Settings.RLSettings = new RLSettingsViewModel();
        OB.Settings.ProxyManagerSettings = new ProxyManagerSettings();
        OB.OBSettings = new OBSettingsViewModel();

        // Load or create settings files
        if (!File.Exists(OB.rlSettingsFile))
        {
            OB.Logger.LogWarning(Components.Main, "RuriLib Settings file not found, generating a default one");
            IOManager.SaveSettings(OB.rlSettingsFile, OB.Settings.RLSettings);
            OB.Logger.LogInfo(Components.Main, $"Created the default RuriLib Settings file {OB.rlSettingsFile}");
        }
        else
        {
            OB.Settings.RLSettings = IOManager.LoadSettings<RLSettingsViewModel>(OB.rlSettingsFile);
            OB.Logger.LogInfo(Components.Main, "Loaded the existing RuriLib Settings file");
        }

        if (!File.Exists(OB.proxyManagerSettingsFile))
        {
            OB.Logger.LogWarning(Components.Main, "Proxy manager Settings file not found, generating a default one");
            OB.Settings.ProxyManagerSettings.ProxySiteUrls.Add(OB.defaultProxySiteUrl);
            OB.Settings.ProxyManagerSettings.ActiveProxySiteUrl = OB.defaultProxySiteUrl;
            OB.Settings.ProxyManagerSettings.ProxyKeys.Add(OB.defaultProxyKey);
            OB.Settings.ProxyManagerSettings.ActiveProxyKey = OB.defaultProxyKey;
            IOManager.SaveSettings(OB.proxyManagerSettingsFile, OB.Settings.ProxyManagerSettings);
            OB.Logger.LogInfo(Components.Main, $"Created the default proxy manager Settings file {OB.proxyManagerSettingsFile}");
        }
        else
        {
            OB.Settings.ProxyManagerSettings = IOManager.LoadSettings<ProxyManagerSettings>(OB.proxyManagerSettingsFile);
            OB.Logger.LogInfo(Components.Main, "Loaded the existing proxy manager Settings file");
        }

        if (!File.Exists(OB.obSettingsFile))
        {
            OB.Logger.LogWarning(Components.Main, "OpenBullet Settings file not found, generating a default one");
            OBIOManager.SaveSettings(OB.obSettingsFile, OB.OBSettings);
            OB.Logger.LogInfo(Components.Main, $"Created the default OpenBullet Settings file {OB.obSettingsFile}");
        }
        else
        {
            OB.OBSettings = OBIOManager.LoadSettings(OB.obSettingsFile);
            OB.Logger.LogInfo(Components.Main, "Loaded the existing OpenBullet Settings file");
        }

        HookSettingsAutoSave();

        // Backup DB if needed
        try
        {
            if (OB.OBSettings.General.BackupDB &&
                (!File.Exists(OB.dataBaseBackupFile) ||
                (File.Exists(OB.dataBaseBackupFile) && (DateTime.Now - File.GetCreationTime(OB.dataBaseBackupFile)).TotalDays > 1)))
            {
                using (var db = new LiteDB.LiteDatabase(OB.dataBaseFile))
                {
                    var coll = db.GetCollection<RuriLib.Models.CProxy>("proxies");
                }

                File.Delete(OB.dataBaseBackupFile);
                File.Copy(OB.dataBaseFile, OB.dataBaseBackupFile);
                OB.Logger.LogInfo(Components.Main, "Backed up the DB");
            }
        }
        catch (Exception ex)
        {
            OB.Logger.LogError(Components.Main, $"Could not backup the DB: {ex.Message}");
        }

        // Load Plugins
        var (plugins, blockPlugins) = Loader.LoadPlugins(OB.pluginsFolder);
        OB.BlockPlugins = blockPlugins.ToList();

        // Block mappings — PageType placeholder until StackerBlock views are built
        OB.BlockMappings = new System.Collections.Generic.List<(Type, Type, Color)>()
        {
            ( typeof(BlockBypassCF),        typeof(UserControl), Colors.DarkSalmon ),
            ( typeof(BlockImageCaptcha),    typeof(UserControl), Colors.DarkOrange ),
            ( typeof(BlockReportCaptcha),   typeof(UserControl), Colors.DarkOrange ),
            ( typeof(BlockFunction),        typeof(UserControl), Colors.YellowGreen ),
            ( typeof(BlockKeycheck),        typeof(UserControl), Colors.DodgerBlue ),
            ( typeof(BlockLSCode),          typeof(UserControl), Colors.White ),
            ( typeof(BlockScript),          typeof(UserControl), Colors.MediumOrchid ),
            ( typeof(BlockParse),           typeof(UserControl), Colors.Gold ),
            ( typeof(BlockRecaptcha),       typeof(UserControl), Colors.Turquoise ),
            ( typeof(BlockSolveCaptcha),    typeof(UserControl), Colors.Turquoise ),
            ( typeof(BlockRequest),         typeof(UserControl), Colors.LimeGreen ),
            ( typeof(RuriLib.Blocks.BlockCookieContainer), typeof(UserControl), Colors.PaleVioletRed ),
            ( typeof(BlockTCP),             typeof(UserControl), Colors.MediumPurple ),
            ( typeof(BlockUtility),         typeof(UserControl), Colors.Wheat ),
            ( typeof(SBlockBrowserAction),  typeof(UserControl), Colors.Green ),
            ( typeof(SBlockElementAction),  typeof(UserControl), Colors.Firebrick ),
            ( typeof(SBlockExecuteJS),      typeof(UserControl), Colors.Indigo ),
            ( typeof(SBlockNavigate),       typeof(UserControl), Colors.RoyalBlue )
        };

        // Add block plugins to mappings
        foreach (var plugin in blockPlugins)
        {
            try
            {
                var color = ParseColorString(plugin.Color);
                OB.BlockMappings.Add((plugin.GetType(), typeof(UserControl), color));
                BlockParser.BlockMappings.Add(plugin.Name, plugin.GetType());
                OB.Logger.LogInfo(Components.Main, $"Initialized {plugin.Name} block plugin");
            }
            catch
            {
                OB.Logger.LogError(Components.Main, $"The color {plugin.Color} in block plugin {plugin.Name} is invalid", true);
            }
        }

        // ViewModels
        OB.RunnerManager = new RunnerManagerViewModel();
        OB.ProxyManager = new ProxyManagerViewModel();
        OB.WordlistManager = new WordlistManagerViewModel();
        OB.CookieManager = new CookieManagerViewModel();
        OB.ConfigManager = new ConfigManagerViewModel();
        OB.HitsDB = new HitsDBViewModel();

        OB.Logger.LogInfo(Components.Main, "All managers initialized");

        // Start the embedded MCP server (async, non-blocking)
        Mcp.McpGlobals.StartTime = DateTime.UtcNow;
        _ = Task.Run(Mcp.McpServerHost.StartAsync);
    }

    // ─── Settings auto-save: persist on any change (debounced) + on exit ───
    private static int _settingsSaveQueued;

    private void HookSettingsAutoSave()
    {
        void Hook(System.ComponentModel.INotifyPropertyChanged vm) =>
            vm.PropertyChanged += (_, _) => QueueSettingsSave();

        Hook(OB.OBSettings.General);
        Hook(OB.OBSettings.Sounds);
        Hook(OB.OBSettings.Sources);
        Hook(OB.OBSettings.Themes);
        Hook(OB.OBSettings.Mcp);
        Hook(OB.Settings.RLSettings.General);
        Hook(OB.Settings.RLSettings.Proxies);
        Hook(OB.Settings.RLSettings.Captchas);
        Hook(OB.Settings.RLSettings.Selenium);
        Hook(OB.Settings.ProxyManagerSettings);

        OB.OBSettings.Sources.Sources.CollectionChanged += (_, _) => QueueSettingsSave();
        OB.Settings.ProxyManagerSettings.ProxySiteUrls.CollectionChanged += (_, _) => QueueSettingsSave();
        OB.Settings.ProxyManagerSettings.ProxyKeys.CollectionChanged += (_, _) => QueueSettingsSave();
    }

    private static void QueueSettingsSave()
    {
        if (System.Threading.Interlocked.Exchange(ref _settingsSaveQueued, 1) == 1) return;
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            System.Threading.Interlocked.Exchange(ref _settingsSaveQueued, 0);
            SaveAllSettings();
        });
    }

    public static void SaveAllSettings()
    {
        try { OBIOManager.SaveSettings(OB.obSettingsFile, OB.OBSettings); } catch { }
        try { RuriLib.IOManager.SaveSettings(OB.rlSettingsFile, OB.Settings.RLSettings); } catch { }
        try { RuriLib.IOManager.SaveSettings(OB.proxyManagerSettingsFile, OB.Settings.ProxyManagerSettings); } catch { }
    }

    private static Color ParseColorString(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr))
            return Colors.White;

        // Try hex format
        if (colorStr.StartsWith("#"))
        {
            var hex = colorStr.TrimStart('#');
            if (hex.Length == 6)
            {
                return new Color(255,
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            }
            if (hex.Length == 8)
            {
                return new Color(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
            }
        }

        // Try named color via reflection
        var prop = typeof(Colors).GetField(colorStr,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);
        if (prop != null)
            return (Color)prop.GetValue(null);

        return Colors.White;
    }
}
