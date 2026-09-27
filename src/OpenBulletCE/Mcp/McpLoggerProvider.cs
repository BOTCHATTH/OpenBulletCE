using Microsoft.Extensions.Logging;
using RuriLib.Models;

namespace OpenBulletCE.Mcp;

/// <summary>
/// Forwards MCP web app logs to the app logger so middleware errors surface
/// in the OpenBullet CE log instead of being swallowed.
/// </summary>
public sealed class McpLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new McpLogger(categoryName);
    public void Dispose() { }
}

public sealed class McpLogger : ILogger
{
    private readonly string _categoryName;
    public McpLogger(string categoryName) => _categoryName = categoryName;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = $"[MCP:{_categoryName}] {formatter(state, exception)}";
        var level = logLevel switch
        {
            LogLevel.Error or LogLevel.Critical => RuriLib.LogLevel.Error,
            LogLevel.Warning => RuriLib.LogLevel.Warning,
            _ => RuriLib.LogLevel.Info
        };
        OB.Logger.Log(Components.Main, level, message);
    }
}
