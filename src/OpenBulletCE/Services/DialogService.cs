using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBulletCE.Services;

public class DialogService : IDialogService
{
    private readonly Window _parent;

    public DialogService(Window parent)
    {
        _parent = parent;
    }

    public async Task<bool> ConfirmAsync(string message, string title)
    {
        var dialog = new ConfirmDialog(message, title);
        return await dialog.ShowDialog<bool>(_parent);
    }

    public async Task AlertAsync(string message, string title)
    {
        var dialog = new AlertDialog(message, title);
        await dialog.ShowDialog(_parent);
    }

    public async Task<string[]> OpenFilePickerAsync(string title, string filterName, params string[] extensions)
    {
        var files = await _parent.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(filterName) { Patterns = extensions.Select(e => $"*.{e}").ToArray() }
            }
        });

        return files.Select(f => f.Path.LocalPath).ToArray();
    }

    public async Task<string> OpenFolderPickerAsync(string title)
    {
        var folders = await _parent.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    public async Task<string> SaveFilePickerAsync(string title, string defaultFileName, string filterName, params string[] extensions)
    {
        var file = await _parent.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(filterName) { Patterns = extensions.Select(e => $"*.{e}").ToArray() }
            }
        });

        return file?.Path.LocalPath;
    }
}

// Simple built-in dialogs (no external deps)
public class ConfirmDialog : Window
{
    private bool _result = false;

    public ConfirmDialog(string message, string title)
    {
        Title = title;
        Width = 400;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        ShowInTaskbar = false;

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        var yes = new Button { Content = "Yes", MinWidth = 80 };
        var no = new Button { Content = "No", MinWidth = 80 };

        yes.Click += (s, e) => { _result = true; Close(true); };
        no.Click += (s, e) => { _result = false; Close(false); };

        buttons.Children.Add(yes);
        buttons.Children.Add(no);
        panel.Children.Add(buttons);
        Content = panel;
    }
}

public class AlertDialog : Window
{
    public AlertDialog(string message, string title)
    {
        Title = title;
        Width = 400;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        ShowInTaskbar = false;

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        var ok = new Button { Content = "OK", MinWidth = 80, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        ok.Click += (s, e) => Close();

        panel.Children.Add(ok);
        Content = panel;
    }
}
