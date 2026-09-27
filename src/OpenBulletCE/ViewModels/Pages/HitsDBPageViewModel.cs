using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RuriLib.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBulletCE.ViewModels.Pages;

public partial class HitsDBPageViewModel : ViewModelBase
{
    public HitsDBViewModel Manager => OB.HitsDB;

    public ObservableCollection<Hit> Hits => Manager.HitsCollection;
    public int Total => Manager.Total;
    public int Filtered => Manager.Filtered;
    public System.Collections.Generic.List<string> ConfigsList => Manager.ConfigsList;

    [ObservableProperty]
    private string _searchString = "";

    [ObservableProperty]
    private string _typeFilter = "SUCCESS";

    [ObservableProperty]
    private string _configFilter = HitsDBViewModel.defaultFilter;

    public HitsDBPageViewModel()
    {
        Manager.RefreshList();
    }

    partial void OnSearchStringChanged(string value)
    {
        Manager.SearchString = value;
        OnPropertyChanged(nameof(Filtered));
    }

    partial void OnTypeFilterChanged(string value)
    {
        Manager.TypeFilter = value;
        OnPropertyChanged(nameof(Filtered));
    }

    partial void OnConfigFilterChanged(string value)
    {
        Manager.ConfigFilter = value;
        OnPropertyChanged(nameof(Filtered));
    }

    [RelayCommand]
    private void Refresh()
    {
        Manager.RefreshList();
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Filtered));
        OnPropertyChanged(nameof(ConfigsList));
    }

    [RelayCommand]
    private void DeleteDuplicates()
    {
        Manager.DeleteDuplicates();
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Filtered));
    }

    [RelayCommand]
    private void DeleteFiltered()
    {
        Manager.DeleteFiltered();
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Filtered));
    }

    [RelayCommand]
    private void PurgeDB()
    {
        Manager.RemoveAll();
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Filtered));
    }
}
