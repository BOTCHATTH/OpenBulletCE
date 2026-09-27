using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace OpenBulletCE.ViewModels;

public partial class LogViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<LogEntryViewModel> _entries = new();

    public void Log(string component, string message, string level = "Info")
    {
        Entries.Add(new LogEntryViewModel
        {
            Component = component,
            Message = message,
            Level = level,
            Time = DateTime.Now
        });
    }
}

public class LogEntryViewModel
{
    public string Component { get; set; } = "";
    public string Message { get; set; } = "";
    public string Level { get; set; } = "Info";
    public DateTime Time { get; set; }
}
