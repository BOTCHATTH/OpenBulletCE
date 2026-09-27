using OpenBulletCE.Repositories;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.Utils;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace OpenBulletCE.ViewModels;

public class CookieManagerViewModel : ViewModelBase, ICookieManager
{
    private LiteDBRepository<Cookie> _repo;

    private List<Cookie> _allCookies = new();
    private ObservableCollection<Cookie> _filtered = new();

    // The view binds to this — pre-filtered
    public ObservableCollection<Cookie> CookiesCollection => _filtered;

    public int Total => _filtered.Count;

    public IEnumerable<Cookie> Cookies => _allCookies;

    public CookieManagerViewModel()
    {
        _repo = new LiteDBRepository<Cookie>(OB.dataBaseFile, "cookies");
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
        foreach (var c in _allCookies.Where(PassesFilter))
            _filtered.Add(c);
    }

    private bool PassesFilter(Cookie item)
        => item.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
    #endregion

    public Cookie GetCookieByName(string name)
        => _allCookies.First(x => x.Name == name);

    public static Cookie FileToCookielist(string path)
    {
        // Folder pickers may return a trailing separator — trim it so the
        // folder's own name is used instead of an empty string.
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileNameWithoutExtension(trimmed);
        if (string.IsNullOrWhiteSpace(name)) name = trimmed;
        var pathAllCookieFiles = ParseCookieFiles.Parse(path);
        return new Cookie(name, path, pathAllCookieFiles);
    }

    #region CRUD Operations
    public void Add(Cookie cookie)
    {
        if (_allCookies.Any(w => w.PathOwnerFolder == cookie.PathOwnerFolder))
            throw new Exception($"Cookie already present: {cookie.PathOwnerFolder}");

        _allCookies.Add(cookie);
        ApplyFilter();
        OnPropertyChanged(nameof(Total));
        _repo.Add(cookie);
    }

    public void RefreshList()
    {
        _allCookies = _repo.Get().ToList();

        // Backfill names for entries saved before the trailing-slash fix
        foreach (var c in _allCookies.Where(c => string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.PathOwnerFolder)))
        {
            var t = c.PathOwnerFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            c.Name = Path.GetFileNameWithoutExtension(t);
            if (string.IsNullOrWhiteSpace(c.Name)) c.Name = t;
            try { _repo.Update(c); } catch { }
        }

        ApplyFilter();
        OnPropertyChanged(nameof(Total));
    }

    public void Update(Cookie cookie)
    {
        _repo.Update(cookie);
    }

    public void Remove(Cookie cookie)
    {
        _allCookies.Remove(cookie);
        _filtered.Remove(cookie);
        OnPropertyChanged(nameof(Total));
        _repo.Remove(cookie);
    }

    public void RemoveAll()
    {
        _allCookies.Clear();
        _filtered.Clear();
        OnPropertyChanged(nameof(Total));
        _repo.RemoveAll();
    }
    #endregion

    #region Delete methods
    public void DeleteNotFound()
    {
        var toRemove = _allCookies.Where(c => !Directory.Exists(c.PathOwnerFolder)).ToList();
        foreach (var c in toRemove)
            Remove(c);
    }
    #endregion
}
