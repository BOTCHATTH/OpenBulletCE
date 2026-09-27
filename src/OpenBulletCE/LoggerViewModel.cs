using Avalonia.Threading;
using RuriLib;
using RuriLib.Interfaces;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace OpenBulletCE;

public enum Components
{
    Main,
    RunnerManager,
    Runner,
    ProxyManager,
    WordlistManager,
    CookieManager,
    HitsDB,
    ConfigManager,
    Stacker,
    OtherOptions,
    Settings,
    ListGenerator,
    SeleniumTools,
    Database,
    About,
    Unknown
}

public class LoggerViewModel : ViewModelBase, ILogger
{
    private readonly ObservableCollection<LogEntry> _entries = new();
    private readonly ObservableCollection<LogEntry> _filtered = new();

    // The view binds to this — pre-filtered
    public ObservableCollection<LogEntry> EntriesCollection => _filtered;

    public IEnumerable<LogEntry> Entries => _entries;

    public bool Enabled
    {
        get
        {
            try { return OB.OBSettings.General.EnableLogging; }
            catch { return false; }
        }
    }

    public int BufferSize
    {
        get
        {
            try { return OB.OBSettings.General.LogBufferSize; }
            catch { return 0; }
        }
    }

    public LoggerViewModel()
    {
        Refresh();
    }

    public void Refresh()
    {
        _filtered.Clear();
        foreach (var entry in _entries.Where(PassesFilter))
            _filtered.Add(entry);
    }

    #region Filters
    private bool _onlyErrors;
    public bool OnlyErrors
    {
        get => _onlyErrors;
        set { _onlyErrors = value; OnPropertyChanged(); Refresh(); }
    }

    private string _searchString = "";
    public string SearchString
    {
        get => _searchString;
        set { _searchString = value; OnPropertyChanged(); Refresh(); }
    }

    private bool PassesFilter(LogEntry entry)
    {
        if (!string.IsNullOrEmpty(SearchString) &&
            !entry.LogString.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            return false;

        if (_onlyErrors)
            return entry.LogLevel == LogLevel.Error;

        return true;
    }
    #endregion

    public void Log(string message, LogLevel level, bool prompt = false, int timeout = 0)
        => Log(Components.Unknown, level, message, prompt, timeout);

    public void Log(Components component, LogLevel level, string message, bool prompt = false, int timeout = 0)
    {
        if (prompt && Alerter.Dialogs != null)
        {
            _ = Alerter.Dialogs.AlertAsync(message, level.ToString());
        }

        if (!Enabled)
            return;

        var entry = new LogEntry(component.ToString(), message, level);

        if (Dispatcher.UIThread.CheckAccess())
        {
            InsertEntry(entry);
            LogToFile(entry);
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                InsertEntry(entry);
                LogToFile(entry);
            });
        }
    }

    public void LogInfo(Components component, string message, bool prompt = false, int timeout = 0)
        => Log(component, LogLevel.Info, message, prompt, timeout);

    public void LogWarning(Components component, string message, bool prompt = false, int timeout = 0)
        => Log(component, LogLevel.Warning, message, prompt, timeout);

    public void LogError(Components component, string message, bool prompt = false, int timeout = 0)
        => Log(component, LogLevel.Error, message, prompt, timeout);

    private void InsertEntry(LogEntry entry)
    {
        try
        {
            _entries.Insert(0, entry);

            if (PassesFilter(entry))
                _filtered.Insert(0, entry);

            while (_entries.Count > BufferSize && _entries.Count > 0)
            {
                var last = _entries[^1];
                _entries.RemoveAt(_entries.Count - 1);
                _filtered.Remove(last);
            }

            while (_filtered.Count > BufferSize && _filtered.Count > 0)
                _filtered.RemoveAt(_filtered.Count - 1);
        }
        catch { }
    }

    public void Clear()
    {
        _entries.Clear();
        _filtered.Clear();
    }

    private static void LogToFile(LogEntry entry)
    {
        try
        {
            if (OB.OBSettings.General.LogToFile)
            {
                File.AppendAllText(OB.logFile,
                    $"[{entry.LogTime}] ({entry.LogLevel}) {entry.LogComponent} - {entry.LogString}{Environment.NewLine}");
            }
        }
        catch { }
    }
}
