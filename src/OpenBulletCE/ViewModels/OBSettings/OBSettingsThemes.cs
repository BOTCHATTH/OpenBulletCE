using RuriLib.ViewModels;
using System.Collections.Generic;
using System.Reflection;

namespace OpenBulletCE.ViewModels;

public class OBSettingsThemes : ViewModelBase
{
    // BACKGROUND
    private string _backgroundMain = "#0F0F12";
    public string BackgroundMain { get => _backgroundMain; set { _backgroundMain = value; OnPropertyChanged(); } }

    private string _backgroundSecondary = "#16161B";
    public string BackgroundSecondary { get => _backgroundSecondary; set { _backgroundSecondary = value; OnPropertyChanged(); } }

    private string _backgroundInput = "#1F2028";
    public string BackgroundInput { get => _backgroundInput; set { _backgroundInput = value; OnPropertyChanged(); } }

    // FOREGROUND
    private string _foregroundMain = "#E4E4E7";
    public string ForegroundMain { get => _foregroundMain; set { _foregroundMain = value; OnPropertyChanged(); } }

    private string _foregroundGood = "#34D399";
    public string ForegroundGood { get => _foregroundGood; set { _foregroundGood = value; OnPropertyChanged(); } }

    private string _foregroundBad = "#F87171";
    public string ForegroundBad { get => _foregroundBad; set { _foregroundBad = value; OnPropertyChanged(); } }

    private string _foregroundFree = "#FBBF24";
    public string ForegroundCustom { get => _foregroundFree; set { _foregroundFree = value; OnPropertyChanged(); } }

    private string _foregroundRetry = "#FDE047";
    public string ForegroundRetry { get => _foregroundRetry; set { _foregroundRetry = value; OnPropertyChanged(); } }

    private string _foregroundToCheck = "#67E8F9";
    public string ForegroundToCheck { get => _foregroundToCheck; set { _foregroundToCheck = value; OnPropertyChanged(); } }

    private string _foregroundMenuSelected = "#6C5CE7";
    public string ForegroundMenuSelected { get => _foregroundMenuSelected; set { _foregroundMenuSelected = value; OnPropertyChanged(); } }

    private string _foregroundBanned = "#FF9F43";
    public string ForegroundBanned { get => _foregroundBanned; set { _foregroundBanned = value; OnPropertyChanged(); } }

    private string _foregroundInput = "#E4E4E7";
    public string ForegroundInput { get => _foregroundInput; set { _foregroundInput = value; OnPropertyChanged(); } }

    // BUTTONS
    private string _successButton = "#34D399";
    public string SuccessButton { get => _successButton; set { _successButton = value; OnPropertyChanged(); } }

    private string _primaryButton = "#8FCDFF";
    public string PrimaryButton { get => _primaryButton; set { _primaryButton = value; OnPropertyChanged(); } }

    private string _warningButton = "#FBBF24";
    public string WarningButton { get => _warningButton; set { _warningButton = value; OnPropertyChanged(); } }

    private string _dangerButton = "#F87171";
    public string DangerButton { get => _dangerButton; set { _dangerButton = value; OnPropertyChanged(); } }

    private string _foregroundButton = "#FFFFFF";
    public string ForegroundButton { get => _foregroundButton; set { _foregroundButton = value; OnPropertyChanged(); } }

    private string _backgroundButton = "#252830";
    public string BackgroundButton { get => _backgroundButton; set { _backgroundButton = value; OnPropertyChanged(); } }

    // EDITOR
    private bool _wordWrap;
    public bool WordWrap { get => _wordWrap; set { _wordWrap = value; OnPropertyChanged(); } }

    // IMAGES
    private bool _useImage;
    public bool UseImage { get => _useImage; set { _useImage = value; OnPropertyChanged(); } }

    private string _backgroundImage = "";
    public string BackgroundImage { get => _backgroundImage; set { _backgroundImage = value; OnPropertyChanged(); } }

    private int _backgroundImageOpacity = 100;
    public int BackgroundImageOpacity { get => _backgroundImageOpacity; set { _backgroundImageOpacity = value; OnPropertyChanged(); } }

    private string _backgroundLogo = "";
    public string BackgroundLogo { get => _backgroundLogo; set { _backgroundLogo = value; OnPropertyChanged(); } }

    private bool _enableSnow;
    public bool EnableSnow { get => _enableSnow; set { _enableSnow = value; OnPropertyChanged(); } }

    private int _snowAmount = 100;
    public int SnowAmount { get => _snowAmount; set { _snowAmount = value; OnPropertyChanged(); } }

    private bool _allowTransparency;
    public bool AllowTransparency { get => _allowTransparency; set { _allowTransparency = value; OnPropertyChanged(); } }

    public void Reset()
    {
        OBSettingsThemes def = new OBSettingsThemes();
        IList<PropertyInfo> props = new List<PropertyInfo>(typeof(OBSettingsThemes).GetProperties());

        foreach (PropertyInfo prop in props)
        {
            prop.SetValue(this, prop.GetValue(def, null));
        }
    }
}
