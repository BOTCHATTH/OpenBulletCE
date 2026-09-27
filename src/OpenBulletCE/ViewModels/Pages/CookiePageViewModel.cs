using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RuriLib.Models;
using SharpCompress.Archives;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBulletCE.ViewModels.Pages;

public partial class CookiePageViewModel : ViewModelBase
{
    public CookieManagerViewModel Manager => OB.CookieManager;

    public ObservableCollection<Cookie> Cookies => Manager.CookiesCollection;
    public int Total => Manager.Total;

    [ObservableProperty]
    private string _searchString = "";

    [ObservableProperty]
    private Cookie _selectedItem;

    public CookiePageViewModel()
    {
    }

    partial void OnSearchStringChanged(string value)
    {
        Manager.SearchString = value;
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void Search()
    {
        Manager.SearchString = SearchString;
        Manager.RefreshList();
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem != null)
        {
            Manager.Remove(SelectedItem);
            OnPropertyChanged(nameof(Total));
        }
    }

    [RelayCommand]
    private async Task AddCookieList()
    {
        if (Alerter.Dialogs == null) return;
        var folder = await Alerter.Dialogs.OpenFolderPickerAsync("Select Cookie Folder");
        if (folder == null) return;
        var cookie = CookieManagerViewModel.FileToCookielist(folder);
        Manager.Add(cookie);
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private async Task AddCookieArchive()
    {
        if (Alerter.Dialogs == null) return;
        var files = await Alerter.Dialogs.OpenFilePickerAsync("Select Cookie Archive", "Archives", "zip", "rar", "7z", "tar", "gz");
        if (files == null || files.Length == 0) return;
        var extracted = ExtractArchive(files[0]);
        if (extracted == null) return;
        var cookie = CookieManagerViewModel.FileToCookielist(extracted);
        Manager.Add(cookie);
        OnPropertyChanged(nameof(Total));
    }

    private static string ExtractArchive(string archivePath)
    {
        var baseDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Cookies");
        var name = System.IO.Path.GetFileNameWithoutExtension(archivePath);
        var dest = System.IO.Path.Combine(baseDir, name);

        var i = 1;
        while (System.IO.Directory.Exists(dest))
            dest = System.IO.Path.Combine(baseDir, $"{name}_{i++}");

        try
        {
            System.IO.Directory.CreateDirectory(dest);
            using (var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(archivePath))
                archive.WriteToDirectory(dest, new SharpCompress.Common.ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            return dest;
        }
        catch (System.Exception ex)
        {
            OB.Logger.LogError(Components.CookieManager, $"Could not extract archive: {ex.Message}");
            return null;
        }
    }

    [RelayCommand]
    private void RemoveCookieList(Cookie cookie)
    {
        if (cookie == null) return;
        Manager.Remove(cookie);
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void DeleteNotFound()
    {
        Manager.DeleteNotFound();
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void RemoveAll()
    {
        Manager.RemoveAll();
        OnPropertyChanged(nameof(Total));
    }
}
