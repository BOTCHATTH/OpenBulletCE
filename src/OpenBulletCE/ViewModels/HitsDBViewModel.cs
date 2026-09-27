using OpenBulletCE.Repositories;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBulletCE.ViewModels;

public class HitsDBViewModel : ViewModelBase, IHitsDB
{
    public LiteDBRepository<Hit> _repo;

    private List<Hit> _allHits = new();
    private ObservableCollection<Hit> _filtered = new();

    // The view binds to this — pre-filtered
    public ObservableCollection<Hit> HitsCollection => _filtered;

    public int Total => _filtered.Count;

    public IEnumerable<Hit> Hits => _allHits;

    public HitsDBViewModel()
    {
        _repo = new LiteDBRepository<Hit>(OB.dataBaseFile, "hits");
        ApplyFilter();
    }

    #region Filters
    public static readonly string defaultFilter = "All";

    public List<string> ConfigsList => _allHits.Select(x => x.ConfigName).Distinct().ToList();

    private string _searchString = "";
    public string SearchString
    {
        get => _searchString;
        set
        {
            _searchString = value;
            OnPropertyChanged();
            ApplyFilter();
            OnPropertyChanged(nameof(Filtered));
        }
    }

    private string _typeFilter = "SUCCESS";
    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            _typeFilter = value;
            OnPropertyChanged();
            ApplyFilter();
            OnPropertyChanged(nameof(Filtered));
        }
    }

    private string _configFilter = defaultFilter;
    public string ConfigFilter
    {
        get => _configFilter;
        set
        {
            _configFilter = value;
            OnPropertyChanged();
            ApplyFilter();
            OnPropertyChanged(nameof(Filtered));
        }
    }

    public int Filtered => _filtered.Count;

    private void ApplyFilter()
    {
        _filtered.Clear();
        foreach (var h in _allHits.Where(PassesFilter))
            _filtered.Add(h);
    }

    private bool PassesFilter(Hit item)
    {
        if (item.Type != TypeFilter)
            return false;

        if (ConfigFilter != defaultFilter && item.ConfigName != ConfigFilter)
            return false;

        if (!string.IsNullOrEmpty(SearchString))
            return item.CapturedData.ToCaptureString().Contains(SearchString, StringComparison.OrdinalIgnoreCase);

        return true;
    }
    #endregion

    #region CRUD Operations
    public void Add(Hit hit)
    {
        _allHits.Add(hit);
        if (PassesFilter(hit))
            _filtered.Add(hit);
        _repo.Add(hit);
    }

    public void RefreshList()
    {
        _allHits = _repo.Get().ToList();
        ApplyFilter();
        OnPropertyChanged(nameof(Total));
    }

    public void Update(Hit hit)
    {
        _repo.Update(hit);
    }

    public void Remove(Hit hit)
    {
        _allHits.Remove(hit);
        _filtered.Remove(hit);
        OnPropertyChanged(nameof(Total));
        _repo.Remove(hit);
    }

    public void Remove(IEnumerable<Hit> hits)
    {
        var toRemove = hits.ToArray();
        foreach (var hit in toRemove)
        {
            _allHits.Remove(hit);
            _filtered.Remove(hit);
        }
        OnPropertyChanged(nameof(Total));
        _repo.Remove(toRemove);
    }

    public void RemoveAll()
    {
        _allHits.Clear();
        _filtered.Clear();
        OnPropertyChanged(nameof(Total));
        _repo.RemoveAll();
    }
    #endregion

    #region Delete methods
    public void DeleteDuplicates()
    {
        var duplicates = _allHits
            .GroupBy(h => h.GetHashCode(OB.OBSettings.General.IgnoreWordlistOnHitDedupe))
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.OrderBy(h => h.Date).Reverse().Skip(1))
            .ToList();

        Remove(duplicates);
    }

    public void DeleteFiltered()
    {
        var filtered = _allHits.Where(h =>
            (string.IsNullOrEmpty(SearchString) || h.CapturedString.Contains(SearchString, StringComparison.OrdinalIgnoreCase)) &&
            (ConfigFilter == "All" || h.ConfigName == ConfigFilter) &&
            h.Type == TypeFilter).ToList();

        Remove(filtered);
    }
    #endregion
}
