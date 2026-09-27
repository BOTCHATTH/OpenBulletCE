using System;

namespace OpenBulletCE.Mcp;

/// <summary>
/// Marshal calls that touch UI-bound state (ObservableCollections in managers,
/// CurrentConfig, runner/job lists) onto the Avalonia UI thread. MCP tool
/// handlers run on Kestrel thread-pool threads, so any write to a bound
/// ObservableCollection throws "Call from invalid thread" without this.
/// </summary>
internal static class McpUi
{
    public static T Run<T>(Func<T> f)
        => Avalonia.Threading.Dispatcher.UIThread.CheckAccess()
            ? f()
            : Avalonia.Threading.Dispatcher.UIThread.Invoke(f);

    public static void Run(Action a) => Run<object?>(() => { a(); return null; });
}
