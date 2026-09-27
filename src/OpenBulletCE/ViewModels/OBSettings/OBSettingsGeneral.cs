using RuriLib.ViewModels;
using System.Collections.Generic;
using System.Reflection;

namespace OpenBulletCE.ViewModels;

public class OBSettingsGeneral : ViewModelBase
{
    private bool _displayLoliScriptOnLoad;
    public bool DisplayLoliScriptOnLoad { get => _displayLoliScriptOnLoad; set { _displayLoliScriptOnLoad = value; OnPropertyChanged(); } }

    private bool _recommendedBots = true;
    public bool RecommendedBots { get => _recommendedBots; set { _recommendedBots = value; OnPropertyChanged(); } }

    private int _startingWidth = 800;
    public int StartingWidth { get => _startingWidth; set { _startingWidth = value; OnPropertyChanged(); } }

    private int _startingHeight = 620;
    public int StartingHeight { get => _startingHeight; set { _startingHeight = value; OnPropertyChanged(); } }

    private bool _changeRunnerInterface;
    public bool ChangeRunnerInterface { get => _changeRunnerInterface; set { _changeRunnerInterface = value; OnPropertyChanged(); } }

    private bool _disableQuitWarning;
    public bool DisableQuitWarning { get => _disableQuitWarning; set { _disableQuitWarning = value; OnPropertyChanged(); } }

    private bool _disableNotSavedWarning;
    public bool DisableNotSavedWarning { get => _disableNotSavedWarning; set { _disableNotSavedWarning = value; OnPropertyChanged(); } }

    private string _defaultAuthor = "";
    public string DefaultAuthor { get => _defaultAuthor; set { _defaultAuthor = value; OnPropertyChanged(); } }

    private bool _liveConfigUpdates;
    public bool LiveConfigUpdates { get => _liveConfigUpdates; set { _liveConfigUpdates = value; OnPropertyChanged(); } }

    private bool _disableHTMLView;
    public bool DisableHTMLView { get => _disableHTMLView; set { _disableHTMLView = value; OnPropertyChanged(); } }

    private bool _alwaysOnTop;
    public bool AlwaysOnTop { get => _alwaysOnTop; set { _alwaysOnTop = value; OnPropertyChanged(); } }

    private bool _autoCreateRunner;
    public bool AutoCreateRunner { get => _autoCreateRunner; set { _autoCreateRunner = value; OnPropertyChanged(); } }

    private bool _persistDebuggerLog;
    public bool PersistDebuggerLog { get => _persistDebuggerLog; set { _persistDebuggerLog = value; OnPropertyChanged(); } }

    private bool _disableSyntaxHelper;
    public bool DisableSyntaxHelper { get => _disableSyntaxHelper; set { _disableSyntaxHelper = value; OnPropertyChanged(); } }

    private bool _displayCapturesLast;
    public bool DisplayCapturesLast { get => _displayCapturesLast; set { _displayCapturesLast = value; OnPropertyChanged(); } }

    private bool _disableCopyPasteBlocks;
    public bool DisableCopyPasteBlocks { get => _disableCopyPasteBlocks; set { _disableCopyPasteBlocks = value; OnPropertyChanged(); } }

    private bool _enableLogging;
    public bool EnableLogging { get => _enableLogging; set { _enableLogging = value; OnPropertyChanged(); } }

    private bool _logToFile;
    public bool LogToFile { get => _logToFile; set { _logToFile = value; OnPropertyChanged(); } }

    private int _logBufferSize = 10000;
    public int LogBufferSize { get => _logBufferSize; set { _logBufferSize = value; OnPropertyChanged(); } }

    private bool _backupDB = true;
    public bool BackupDB { get => _backupDB; set { _backupDB = value; OnPropertyChanged(); } }

    private bool _ignoreWordlistOnHitsDedupe;
    public bool IgnoreWordlistOnHitDedupe { get => _ignoreWordlistOnHitsDedupe; set { _ignoreWordlistOnHitsDedupe = value; OnPropertyChanged(); } }

    public void Reset()
    {
        OBSettingsGeneral def = new OBSettingsGeneral();
        IList<PropertyInfo> props = new List<PropertyInfo>(typeof(OBSettingsGeneral).GetProperties());

        foreach (PropertyInfo prop in props)
        {
            prop.SetValue(this, prop.GetValue(def, null));
        }
    }
}
