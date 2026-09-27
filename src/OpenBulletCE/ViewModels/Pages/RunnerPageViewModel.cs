using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RuriLib;
using RuriLib.Runner;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBulletCE.ViewModels.Pages;

public partial class RunnerPageViewModel : ViewModelBase
{
    public RunnerManagerViewModel Manager => OB.RunnerManager;

    public ObservableCollection<RunnerInstance> Runners => Manager.RunnersCollection;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowManager), nameof(ShowDetail))]
    private RunnerInstance _selectedRunner;

    [ObservableProperty]
    private string _logText = "";

    /// <summary>Per-level colored log lines for the console panel.</summary>
    public ObservableCollection<RunnerLogLine> LogLines { get; } = new();

    // The engine raises one MessageArrived per data line (assignment log) plus
    // per-block lines per bot — posting each to the UI thread buries input
    // events behind thousands of queued jobs and the app looks frozen (STOP
    // click, splitters, context menus all starve). Queue them and flush in
    // batches on a timer instead.
    private readonly System.Collections.Concurrent.ConcurrentQueue<RunnerLogLine> _pendingLog = new();
    private Avalonia.Threading.DispatcherTimer _logFlushTimer;

    private Avalonia.Threading.DispatcherTimer LogFlushTimer => _logFlushTimer ??= CreateLogFlushTimer();

    private Avalonia.Threading.DispatcherTimer CreateLogFlushTimer()
    {
        var t = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        t.Tick += (_, _) => FlushPendingLog();
        t.Start();
        return t;
    }

    private void FlushPendingLog()
    {
        var sb = new System.Text.StringBuilder();
        int added = 0;
        while (added < 500 && _pendingLog.TryDequeue(out var line))
        {
            LogLines.Add(line);
            sb.Append(line.Text).Append(Environment.NewLine);
            added++;
        }
        if (added == 0) return;

        LogText += sb.ToString();
        if (LogText.Length > 100_000)
            LogText = LogText[^50_000..];
        while (LogLines.Count > 2000)
            LogLines.RemoveAt(0);
    }

    // Same palette as Themes/Colors.axaml so the console matches the app.
    private static readonly Avalonia.Media.IBrush BrushInfo =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#DCDCDC"));
    private static readonly Avalonia.Media.IBrush BrushWarn =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFF00"));
    private static readonly Avalonia.Media.IBrush BrushErr =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6347"));
    private static readonly Avalonia.Media.IBrush BrushHit =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#ADFF2F"));
    private static readonly Avalonia.Media.IBrush BrushCustomC =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF8C00"));
    private static readonly Avalonia.Media.IBrush BrushToCheck =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7FFFD4"));
    private static readonly Avalonia.Media.IBrush BrushAccent =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8FCDFF"));
    private static readonly Avalonia.Media.IBrush BrushDim =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6B7288"));

    /// <summary>
    /// Colors a log line by severity first, then by content — result lines carry
    /// the status keyword so FAIL reads red, SUCCESS green, etc. Assignment
    /// noise is dimmed so the eye lands on results, not bookkeeping.
    /// </summary>
    private static Avalonia.Media.IBrush MessageBrush(LogLevel level, string message)
    {
        if (level == LogLevel.Error) return BrushErr;
        if (level == LogLevel.Warning) return BrushWarn;

        var m = System.Text.RegularExpressions.Regex.Match(message, @"result (\S+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var r = m.Groups[1].Value.ToUpperInvariant();
            if (r is "SUCCESS" or "HIT") return BrushHit;
            if (r is "FAIL" or "ERROR") return BrushErr;
            if (r is "RETRY" or "BAN") return BrushWarn;
            if (r is "NONE" or "TOCHECK" or "TOCHK") return BrushToCheck;
            return BrushCustomC; // custom status names
        }

        if (message.Contains("Trying to assign") || message.Contains("Assigned data")
            || message.StartsWith("Creating bot") || message.StartsWith("Removing bot")
            || message.StartsWith("Bots Number")) return BrushDim;

        if (message.StartsWith("Started") || message.StartsWith("Loaded")
            || message.StartsWith("Setting up") || message.StartsWith("Using Proxies")
            || message.StartsWith("Sent cancellation") || message.StartsWith("All data assigned"))
            return BrushAccent;

        if (message.Contains("started with data")) return BrushAccent;

        return BrushInfo;
    }

    public bool ShowManager => SelectedRunner == null;
    public bool ShowDetail => SelectedRunner != null;

    partial void OnSelectedRunnerChanged(RunnerInstance? oldValue, RunnerInstance newValue)
    {
        if (oldValue != null)
            oldValue.ViewModel.MessageArrived -= OnRunnerMessage;
        if (newValue != null)
        {
            newValue.ViewModel.MessageArrived += OnRunnerMessage;
            _ = LogFlushTimer; // must be created on the UI thread — never lazily in OnRunnerMessage
            LogText = $"Runner {newValue.Id} selected." + Environment.NewLine;
            LogLines.Clear();
            LogLines.Add(new RunnerLogLine
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Text = $"Runner {newValue.Id} selected.",
                Brush = BrushInfo
            });
        }
    }

    private void OnRunnerMessage(IRunnerMessaging sender, LogLevel level, string message, bool prompt, int timeout)
    {
        _pendingLog.Enqueue(new RunnerLogLine
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Text = message,
            Brush = MessageBrush(level, message)
        });
        // Bound the queue so a long-running job can't grow it unboundedly;
        // the flush timer always catches up.
        while (_pendingLog.Count > 5000)
            _pendingLog.TryDequeue(out _);
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogLines.Clear();
        LogText = "";
    }

    public RunnerPageViewModel()
    {
        // If no runners and auto-create is on, create one
        if (Runners.Count == 0 && OB.OBSettings?.General.AutoCreateRunner == true)
        {
            Manager.Create();
        }

        // Try to restore previous session
        if (Runners.Count == 0)
        {
            Manager.RestoreSession();
        }
    }

    [RelayCommand]
    private void AddRunner()
    {
        Manager.Create();
    }

    [RelayCommand]
    private void CloneRunner(RunnerInstance runner)
    {
        if (runner == null) return;
        var src = runner.ViewModel;
        var clone = Manager.Create() as RunnerViewModel;
        if (clone == null) return;

        try
        {
            if (src.Config != null) clone.SetConfig(src.Config, false);
            if (src.Config?.Settings.Type == RuriLib.Enums.ConfigType.CookieEdition)
            {
                if (src.Cookielist != null) clone.SetCookielist(src.Cookielist);
            }
            else if (src.Wordlist != null) clone.SetWordlist(src.Wordlist);

            clone.BotsAmount = src.BotsAmount;
            clone.StartingPoint = src.StartingPoint;
            clone.ProxyMode = src.ProxyMode;
        }
        catch { }
    }

    [RelayCommand]
    private void RemoveRunner(RunnerInstance runner)
    {
        if (runner != null)
            Manager.Remove(runner.Id);
    }

    [RelayCommand]
    private void RemoveAllRunners()
    {
        Manager.RemoveAll();
    }

    [RelayCommand]
    private void StartAll()
    {
        foreach (var r in Runners)
            r.ViewModel.Start();
    }

    [RelayCommand]
    private void StopAll()
    {
        foreach (var r in Runners)
            r.ViewModel.Stop();
    }

    [RelayCommand]
    private void BackToList()
    {
        SelectedRunner = null;
    }
}

/// <summary>One console line in the runner log panel.</summary>
public sealed class RunnerLogLine
{
    public string Time { get; init; } = "";
    public string Text { get; init; } = "";
    public Avalonia.Media.IBrush Brush { get; init; } = Avalonia.Media.Brushes.White;
}
