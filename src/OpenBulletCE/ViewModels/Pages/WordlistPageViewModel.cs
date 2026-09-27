using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RuriLib.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBulletCE.ViewModels.Pages;

public partial class WordlistPageViewModel : ViewModelBase
{
    public WordlistManagerViewModel Manager => OB.WordlistManager;

    public ObservableCollection<Wordlist> Wordlists => Manager.WordlistsCollection;
    public int Total => Manager.Total;

    [ObservableProperty]
    private string _searchString = "";

    [ObservableProperty]
    private Wordlist _selectedItem;

    public WordlistPageViewModel()
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
    private async Task AddWordlist()
    {
        if (Alerter.Dialogs == null) return;
        var files = await Alerter.Dialogs.OpenFilePickerAsync("Select Wordlist", "Text Files", "txt");
        foreach (var file in files)
        {
            var wordlist = WordlistManagerViewModel.FileToWordlist(file);
            Manager.Add(wordlist);
        }
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void RemoveWordlist(Wordlist wordlist)
    {
        if (wordlist == null) return;
        Manager.Remove(wordlist);
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
