using System.Threading.Tasks;

namespace OpenBulletCE.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string message, string title);
    Task AlertAsync(string message, string title);
    Task<string[]> OpenFilePickerAsync(string title, string filterName, params string[] extensions);
    Task<string> OpenFolderPickerAsync(string title);
    Task<string> SaveFilePickerAsync(string title, string defaultFileName, string filterName, params string[] extensions);
}
