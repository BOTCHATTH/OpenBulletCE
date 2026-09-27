using OpenBulletCE.Repositories;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace OpenBulletCE.ViewModels;

public class WordlistManagerViewModel : ViewModelBase, IWordlistManager
{
    private LiteDBRepository<Wordlist> _repo;

    private List<Wordlist> _allWordlists = new();
    private ObservableCollection<Wordlist> _filtered = new();

    // The view binds to this — pre-filtered
    public ObservableCollection<Wordlist> WordlistsCollection => _filtered;

    public int Total => _filtered.Count;

    public IEnumerable<Wordlist> Wordlists => _allWordlists;

    public WordlistManagerViewModel()
    {
        _repo = new LiteDBRepository<Wordlist>(OB.dataBaseFile, "wordlists");
        RefreshList();
    }

    #region Filters
    private string _searchString = "";
    public string SearchString
    {
        get => _searchString;
        set
        {
            _searchString = value;
            OnPropertyChanged();
            ApplyFilter();
            OnPropertyChanged(nameof(Total));
        }
    }

    private void ApplyFilter()
    {
        _filtered.Clear();
        foreach (var w in _allWordlists.Where(PassesFilter))
            _filtered.Add(w);
    }

    private bool PassesFilter(Wordlist item)
        => item.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
    #endregion

    public Wordlist GetWordlistByName(string name)
        => _allWordlists.First(x => x.Name == name);

    public static Wordlist FileToWordlist(string path)
    {
        var wordlist = new Wordlist(Path.GetFileNameWithoutExtension(path), path,
            OB.Settings.Environment.WordlistTypes.First().Name, "");

        var first = File.ReadLines(wordlist.Path).First();
        wordlist.Type = OB.Settings.Environment.RecognizeWordlistType(first);

        return wordlist;
    }

    #region CRUD Operations
    public void Add(Wordlist wordlist)
    {
        if (_allWordlists.Any(w => w.Path == wordlist.Path))
            throw new Exception($"Wordlist already present: {wordlist.Path}");

        _allWordlists.Add(wordlist);
        ApplyFilter();
        OnPropertyChanged(nameof(Total));
        _repo.Add(wordlist);
    }

    public void RefreshList()
    {
        _allWordlists = _repo.Get().ToList();
        ApplyFilter();
        OnPropertyChanged(nameof(Total));
    }

    public void Update(Wordlist wordlist)
    {
        _repo.Update(wordlist);
    }

    public void Remove(Wordlist wordlist)
    {
        _allWordlists.Remove(wordlist);
        _filtered.Remove(wordlist);
        OnPropertyChanged(nameof(Total));
        _repo.Remove(wordlist);
    }

    public void RemoveAll()
    {
        _allWordlists.Clear();
        _filtered.Clear();
        OnPropertyChanged(nameof(Total));
        _repo.RemoveAll();
    }
    #endregion

    #region Delete methods
    public void DeleteNotFound()
    {
        var toRemove = _allWordlists.Where(w => !File.Exists(w.Path)).ToList();
        foreach (var w in toRemove)
            Remove(w);
    }
    #endregion
}
