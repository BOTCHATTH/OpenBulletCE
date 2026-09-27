using PluginFramework;
using RuriLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBulletCE;

public static class BlocksExtensions
{
    public static IEnumerable<BlockBase> OnlyPlugins(this IEnumerable<BlockBase> blocks)
        => blocks.Where(b => b.IsPlugin());

    public static bool IsPlugin(this BlockBase block)
        => block.GetType().GetInterface(nameof(IBlockPlugin)) == typeof(IBlockPlugin);
}

public static class EnumerableExtensions
{
    public static void SaveToFile<T>(this IEnumerable<T> items, string fileName, Func<T, string> mapping)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException("The filename must not be empty");

        File.WriteAllLines(fileName, items.Select(i => mapping(i)));
    }

    public static async Task CopyToClipboardAsync<T>(this IEnumerable<T> items, Func<T, string> mapping)
    {
        var text = string.Join(Environment.NewLine, items.Select(i => mapping(i)));
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(text);
        }
    }
}
