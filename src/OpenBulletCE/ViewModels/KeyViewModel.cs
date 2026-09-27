using RuriLib;
using RuriLib.Functions.Conditions;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBulletCE.ViewModels;

public class KeyFullId
{
    public int KeyId { get; set; }
    public int ParentId { get; set; }
}

public class KeyViewModel : ViewModelBase
{
    private KeyFullId _id;
    public KeyFullId Id { get => _id; set { _id = value; OnPropertyChanged(); } }

    public Key Key { get; set; }
    public string LeftTerm { get => Key.LeftTerm; set { Key.LeftTerm = value; OnPropertyChanged(); } }
    public Comparer Comparer { get => Key.Comparer; set { Key.Comparer = value; OnPropertyChanged(); } }
    public string RightTerm { get => Key.RightTerm; set { Key.RightTerm = value; OnPropertyChanged(); } }

    public KeyViewModel(Key key, int id, int parentId)
    {
        Key = key;
        Id = new KeyFullId { KeyId = id, ParentId = parentId };
        OnPropertyChanged(nameof(Id));
    }
}
