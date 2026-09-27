using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RuriLib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenBulletCE.ViewModels.Pages;

public partial class ToolsPageViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListGeneratorActive), nameof(IsSeleniumToolsActive), nameof(IsDatabaseActive))]
    private ViewModelBase _currentTool;

    public bool IsListGeneratorActive => CurrentTool is ListGeneratorViewModel;
    public bool IsSeleniumToolsActive => CurrentTool is SeleniumToolsViewModel;
    public bool IsDatabaseActive => CurrentTool is DatabaseViewModel;

    public ToolsPageViewModel()
    {
        _currentTool = new ListGeneratorViewModel();
    }

    [RelayCommand]
    private void OpenTool(string toolName)
    {
        CurrentTool = toolName switch
        {
            "ListGenerator" => new ListGeneratorViewModel(),
            "SeleniumTools" => new SeleniumToolsViewModel(),
            "Database" => new DatabaseViewModel(),
            _ => CurrentTool
        };
    }
}

public partial class ListGeneratorViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _onlyLuhn = false;

    [ObservableProperty]
    private bool _autoImport = false;

    [ObservableProperty]
    private string _mask = "657438923467423847****:**";

    [ObservableProperty]
    private string _allowedCharacters = "0123456789";

    public int OutputLines
    {
        get
        {
            try
            {
                var varCount = Mask.Count(c => c == '*');
                var lines = (int)Math.Pow(AllowedCharacters.Length, varCount);
                var splitMask = Mask.Split(':')[0].Replace("*", "");
                var allNum = !splitMask.Any(c => !char.IsDigit(c)) && !AllowedCharacters.Any(c => !char.IsDigit(c));
                return allNum && OnlyLuhn ? lines / 10 : lines;
            }
            catch { return 0; }
        }
    }

    public string OutputSize => SizeSuffix(sizeof(char) * Mask.Length * OutputLines);

    partial void OnMaskChanged(string value) => RefreshOutput();
    partial void OnAllowedCharactersChanged(string value) => RefreshOutput();
    partial void OnOnlyLuhnChanged(bool value) => RefreshOutput();

    private void RefreshOutput()
    {
        OnPropertyChanged(nameof(OutputLines));
        OnPropertyChanged(nameof(OutputSize));
    }

    [RelayCommand]
    private void AddLowercase() => AllowedCharacters += "abcdefghijklmnopqrstuvwxyz";

    [RelayCommand]
    private void AddUppercase() => AllowedCharacters += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    [RelayCommand]
    private void AddDigits() => AllowedCharacters += "0123456789";

    [RelayCommand]
    private void ClearChars() => AllowedCharacters = "";

    [RelayCommand]
    private async Task Generate()
    {
        if (Alerter.Dialogs == null) return;
        var path = await Alerter.Dialogs.SaveFilePickerAsync("Save Output List", "output.txt", "Text Files", "txt");
        if (string.IsNullOrEmpty(path)) return;

        await Task.Run(() =>
        {
            using var sw = new StreamWriter(path);
            WriteCombinations(sw, Mask);
        });

        if (AutoImport)
        {
            var wordlist = new Wordlist($"Generated_{DateTime.Now:HHmmss}", path, "Default", "");
            OB.WordlistManager.Add(wordlist);
        }
    }

    private void WriteCombinations(StreamWriter sw, string input)
    {
        if (input.Contains('*'))
        {
            foreach (var c in AllowedCharacters)
            {
                var idx = input.IndexOf('*');
                var next = input[..idx] + c + input[(idx + 1)..];
                WriteCombinations(sw, next);
            }
        }
        else
        {
            if (!OnlyLuhn || Luhn(input.Split(':')[0]))
                sw.WriteLine(input);
        }
    }

    public static bool Luhn(string digits)
    {
        return digits.All(char.IsDigit) && digits.Reverse()
            .Select(c => c - 48)
            .Select((thisNum, i) => i % 2 == 0
                ? thisNum
                : ((thisNum *= 2) > 9 ? thisNum - 9 : thisNum)
            ).Sum() % 10 == 0;
    }

    private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB" };

    private static string SizeSuffix(long value)
    {
        if (value == 0) return "0 bytes";
        int mag = (int)Math.Log(value, 1024);
        decimal adjusted = (decimal)value / (1L << (mag * 10));
        return $"{adjusted:n1} {SizeSuffixes[mag]}";
    }
}

public partial class DatabaseViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isShrinking;

    [RelayCommand]
    private async Task Shrink()
    {
        if (IsShrinking) return;
        if (OB.RunnerManager.RunnersCollection.Any(r => r.ViewModel.Master.IsBusy))
        {
            OB.Logger.LogWarning(Components.Database, "Please stop all active runners before shrinking the database!", true);
            return;
        }

        IsShrinking = true;
        try
        {
            await Task.Run(() =>
            {
                using var db = new LiteDB.LiteDatabase(OB.dataBaseFile);
                var previousSize = (int)(new FileInfo(OB.dataBaseFile).Length / 1000);
                db.Rebuild();
                var newSize = (int)(new FileInfo(OB.dataBaseFile).Length / 1000);
                OB.Logger.LogInfo(Components.Database, $"Database successfully shrinked from {previousSize} KB to {newSize} KB", true);
            });
        }
        catch (Exception ex) { OB.Logger.LogError(Components.Database, $"Shrink failed! Error: {ex.Message}"); }
        finally { IsShrinking = false; }
    }
}

public partial class SeleniumToolsViewModel : ViewModelBase
{
    [RelayCommand]
    private void KillChromedrivers()
    {
        KillProcess("chromedriver");
    }

    [RelayCommand]
    private void KillGeckodrivers()
    {
        KillProcess("geckodriver");
    }

    [RelayCommand]
    private void KillChromes()
    {
        KillProcess("chrome");
    }

    [RelayCommand]
    private void KillFirefoxes()
    {
        KillProcess("firefox");
    }

    [RelayCommand]
    private void CleanChromeCache()
    {
        CleanTempFolders("scopeddir", "chromeurlfetcher", "chromeBITS");
    }

    [RelayCommand]
    private void CleanFirefoxCache()
    {
        CleanTempFolders("rust_mozprofile");
    }

    private static void KillProcess(string name)
    {
        try { Process.Start("cmd.exe", $"/C taskkill /F /IM {name}.exe /T"); }
        catch { }
    }

    private static void CleanTempFolders(params string[] patterns)
    {
        var tempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
        if (!Directory.Exists(tempPath)) return;
        foreach (var dir in Directory.GetDirectories(tempPath))
        {
            if (patterns.Any(p => dir.Contains(p)))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
