using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenBulletCE.ViewModels.Pages;

public partial class AboutPageViewModel : ViewModelBase
{
    public string Version => "2.0.0";
    public string BuildDate => "2026";
    public string Description => "OpenBullet Cookie Edition — Avalonia UI";
}
