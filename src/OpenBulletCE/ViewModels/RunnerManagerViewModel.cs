using LiteDB;
using OpenBulletCE.Repositories;
using RuriLib;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.Runner;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using Extreme.Net;

namespace OpenBulletCE.ViewModels;

public class RunnerManagerViewModel : ViewModelBase, IRunnerManager
{
    private LiteDBRepository<RunnerSessionData> _repo;

    public ObservableCollection<RunnerInstance> RunnersCollection { get; set; } = new();

    public IEnumerable<IRunner> Runners => RunnersCollection.Select(i => i.ViewModel);

    private Random rand = new Random();

    public RunnerManagerViewModel()
    {
        _repo = new LiteDBRepository<RunnerSessionData>(OB.dataBaseFile, "runners");
    }

    public RunnerInstance Get(int id)
        => RunnersCollection.First(r => r.Id == id);

    public IRunner Create()
    {
        var instance = new RunnerInstance(rand.Next());
        var vm = instance.ViewModel;

        vm.ConfigChanged += OnRunnerSessionChanged;
        vm.ListChanged += OnRunnerSessionChanged;

        // Wire up the runner's messaging events (previously done in the View ctor)
        vm.MessageArrived += (sender, level, message, prompt, timeout) =>
            OB.Logger.Log(Components.Runner, level, message, prompt, timeout);

        vm.WorkerStatusChanged += (sender) =>
        {
            var runner = sender as RunnerViewModel;
            switch (runner.Master.Status)
            {
                case WorkerStatus.Running:
                    OB.Logger.LogInfo(Components.Runner, $"Started Running Config {runner.ConfigName} with Wordlist {runner.WordlistName}");
                    break;
                case WorkerStatus.Stopping:
                    OB.Logger.LogInfo(Components.Runner, "Sent Abort Request");
                    break;
                case WorkerStatus.Idle:
                    OB.Logger.LogInfo(Components.Runner, "Runner Idle");
                    break;
            }
        };

        vm.DispatchAction += (sender, action) =>
            Dispatcher.UIThread.Post(action);

        vm.FoundHit += (sender, hit) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (vm.Config != null && vm.Config.Settings.SaveHitsToTextFile)
                    {
                        var folderName = Path.Combine("Hits",
                            RuriLib.Functions.Files.Files.MakeValidFileName(vm.Config.Settings.Name));
                        if (!Directory.Exists(folderName))
                            Directory.CreateDirectory(folderName);
                        var fileName = Path.Combine(folderName, $"{hit.Type}.txt");
                        File.AppendAllText(fileName, $"{hit.Data} | {hit.CapturedString}{Environment.NewLine}");
                    }
                    else
                    {
                        OB.HitsDB.Add(hit);
                    }
                }
                catch (Exception ex)
                {
                    OB.Logger.LogError(Components.Runner, $"Failed to add hit {hit.Data}: {ex.Message}");
                }
            });
        };

        vm.ReloadProxies += (sender) =>
        {
            var proxies = OB.ProxyManager.ProxiesCollection.ToList();
            List<CProxy> toAdd;
            if (vm.Config.Settings.OnlySocks) toAdd = proxies.Where(x => x.Type != ProxyType.Http).ToList();
            else if (vm.Config.Settings.OnlySsl) toAdd = proxies.Where(x => x.Type == ProxyType.Http).ToList();
            else toAdd = proxies;
            vm.ProxyPool = new ProxyPool(toAdd, OB.Settings.RLSettings.Proxies.ShuffleOnStart);
        };

        vm.SaveProgress += (sender) =>
        {
            switch (vm.Config?.Settings.Type)
            {
                case RuriLib.Enums.ConfigType.Default:
                    SaveRecord(vm.Config, vm.Wordlist, vm.TestedCount + vm.StartingPoint);
                    break;
                case RuriLib.Enums.ConfigType.CookieEdition:
                    SaveRecord(vm.Config, vm.Cookielist, vm.TestedCount + vm.StartingPoint);
                    break;
            }
        };

        vm.AskCustomInputs += (sender) =>
        {
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    vm.CustomInputs = new List<KeyValuePair<string, string>>();
                    var window = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow : null;
                    if (window == null) { vm.CustomInputsInitialized = true; return; }

                    var inputs = vm.Config.Settings.CustomInputs
                        .Select(c => new Views.Dialogs.CustomInputAnswer
                        {
                            VariableName = c.VariableName,
                            Value = ""
                        }).ToList();

                    var dialog = new Views.Dialogs.DialogCustomInput(inputs,
                        string.Join(", ", vm.Config.Settings.CustomInputs.Select(c => c.Description)));
                    var result = await dialog.ShowDialog<bool>(window);
                    if (result && dialog.Answers != null)
                    {
                        foreach (var answer in dialog.Answers)
                            vm.CustomInputs.Add(new KeyValuePair<string, string>(answer.VariableName, answer.Value));
                    }
                    vm.CustomInputsInitialized = true;
                }
                catch { vm.CustomInputsInitialized = true; }
            });
        };

        RunnersCollection.Add(instance);
        return instance.ViewModel;
    }

    public void Remove(IRunner runner)
        => RunnersCollection.Remove(RunnersCollection.First(r => r.ViewModel == runner));

    public void Remove(int id)
        => RunnersCollection.Remove(Get(id));

    public void RemoveAll()
        => RunnersCollection.Clear();

    public void OnRunnerSessionChanged(IRunnerMessaging obj)
        => SaveSession();

    public void SaveSession()
    {
        _repo.RemoveAll();
        _repo.Add(RunnersCollection
            .Select(r => r.ViewModel)
            .Select(r => new RunnerSessionData()
            {
                Bots = r.BotsAmount,
                Config = r.Config != null ? r.ConfigName : "",
                Wordlist = r.Wordlist != null ? r.Wordlist.Path : "",
                Cookielist = r.Cookielist != null ? r.Cookielist.PathOwnerFolder : "",
                ProxyMode = r.ProxyMode
            }));
    }

    public bool RestoreSession()
    {
        var runners = _repo.Get().ToArray();
        if (runners.Length == 0) return false;

        foreach (var r in runners)
        {
            try
            {
                var instance = Create();
                instance.BotsAmount = r.Bots;
                instance.ProxyMode = r.ProxyMode;

                var configVM = OB.ConfigManager.Configs.FirstOrDefault(c => c.Name == r.Config)
                    ?? throw new Exception($"The Config {r.Config} was not found in the ConfigManager");

                instance.SetConfig(configVM.Config, false);

                switch (configVM.Type)
                {
                    case RuriLib.Enums.ConfigType.Default:
                        var wordlist = OB.WordlistManager.Wordlists.FirstOrDefault(w => w.Path == r.Wordlist);
                        if (wordlist == null)
                        {
                            if (!File.Exists(r.Wordlist))
                                throw new Exception($"The Wordlist {r.Wordlist} was not found in the WordlistManager or on Disk");

                            wordlist = WordlistManagerViewModel.FileToWordlist(r.Wordlist);
                        }
                        instance.SetWordlist(wordlist);
                        instance.StartingPoint = RetrieveRecord(configVM.Config, wordlist);
                        break;

                    case RuriLib.Enums.ConfigType.CookieEdition:
                        var cookielist = OB.CookieManager.Cookies.FirstOrDefault(w => w.PathOwnerFolder == r.Cookielist);
                        if (cookielist == null)
                        {
                            if (!Directory.Exists(r.Cookielist))
                                throw new Exception($"The Cookie list {r.Cookielist} was not found in the WordlistManager or on Disk");

                            cookielist = CookieManagerViewModel.FileToCookielist(r.Cookielist);
                        }
                        instance.SetCookielist(cookielist);
                        instance.StartingPoint = RetrieveRecord(configVM.Config, cookielist);
                        break;
                }
            }
            catch (Exception ex)
            {
                OB.Logger.LogError(Components.RunnerManager, ex.Message);
            }
        }

        return true;
    }

    public int RetrieveRecord(Config config, Wordlist wordlist)
    {
        if (wordlist == null || config == null) return 1;

        using (var db = new LiteDatabase(OB.dataBaseFile))
        {
            var record = db.GetCollection<Record>("records")
                .FindOne(r => r.ConfigName == config.Settings.Name && r.WordlistLocation == wordlist.Path);
            return record?.Checkpoint ?? 1;
        }
    }

    public int RetrieveRecord(Config config, Cookie cookielist)
    {
        if (cookielist == null || config == null) return 1;

        using (var db = new LiteDatabase(OB.dataBaseFile))
        {
            var record = db.GetCollection<Record>("records")
                .FindOne(r => r.ConfigName == config.Settings.Name && r.CookielistLocation == cookielist.PathOwnerFolder);
            return record?.Checkpoint ?? 1;
        }
    }

    public void SaveRecord(Config config, Wordlist wordlist, int progress)
    {
        if (config == null || wordlist == null) return;

        using (var db = new LiteDatabase(OB.dataBaseFile))
        {
            var coll = db.GetCollection<Record>("records");
            var record = new Record(config.Settings.Name, wordlist.Path, "", progress);

            coll.DeleteMany(r => r.ConfigName == config.Settings.Name && r.WordlistLocation == wordlist.Path);
            coll.Insert(record);
        }
    }

    public void SaveRecord(Config config, Cookie cookielist, int progress)
    {
        if (config == null || cookielist == null) return;

        using (var db = new LiteDatabase(OB.dataBaseFile))
        {
            var coll = db.GetCollection<Record>("records");
            var record = new Record(config.Settings.Name, "", cookielist.PathOwnerFolder, progress);

            coll.DeleteMany(r => r.ConfigName == config.Settings.Name && r.CookielistLocation == cookielist.PathOwnerFolder);
            coll.Insert(record);
        }
    }
}

public class RunnerInstance
{
    // View is resolved via ViewLocator — no direct View reference needed
    public RunnerViewModel ViewModel { get; private set; }
    public int Id { get; set; }

    public CommunityToolkit.Mvvm.Input.RelayCommand StartCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand StopCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand ForceStopCommand { get; }

    public RunnerInstance(int id)
    {
        Id = id;
        ViewModel = new RunnerViewModel(OB.Settings.Environment, OB.Settings.RLSettings);

        StartCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => ViewModel.Start());
        StopCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => ViewModel.Stop());
        ForceStopCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => ViewModel.ForceStop());
    }
}
