using RuriLib.ViewModels;
using System.Collections.Generic;
using System.Reflection;

namespace OpenBulletCE.ViewModels;

public class OBSettingsSounds : ViewModelBase
{
    private bool _enableSounds;
    public bool EnableSounds { get => _enableSounds; set { _enableSounds = value; OnPropertyChanged(); } }

    private string _onHitSound = "rifle_hit.wav";
    public string OnHitSound { get => _onHitSound; set { _onHitSound = value; OnPropertyChanged(); } }

    private string _onReloadSound = "rifle_reload.wav";
    public string OnReloadSound { get => _onReloadSound; set { _onReloadSound = value; OnPropertyChanged(); } }

    public void Reset()
    {
        OBSettingsSounds def = new OBSettingsSounds();
        IList<PropertyInfo> props = new List<PropertyInfo>(typeof(OBSettingsSounds).GetProperties());

        foreach (PropertyInfo prop in props)
        {
            prop.SetValue(this, prop.GetValue(def, null));
        }
    }
}
