using OpenBulletCE.Services;
using RuriLib.Interfaces;
using System.Threading.Tasks;

namespace OpenBulletCE;

public class Alerter : IAlerter
{
    public static IDialogService Dialogs { get; set; }

    public bool YesOrNo(string message, string title)
    {
        if (Dialogs == null)
            return false;

        // Called synchronously from background threads — block on the dialog
        return Dialogs.ConfirmAsync(message, title).GetAwaiter().GetResult();
    }
}
