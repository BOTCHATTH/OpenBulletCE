using System;
using Microsoft.Extensions.DependencyInjection;

namespace OpenBulletCE.Mcp;

/// <summary>
/// Global state shared with MCP tools.
/// </summary>
internal static class McpGlobals
{
    /// <summary>When the native application was started.</summary>
    public static DateTime StartTime { get; set; }

    /// <summary>
    /// The MCP server's DI service provider, set when the WebApplication is built.
    /// Used as a fallback service locator when the MCP framework doesn't inject
    /// service parameters (e.g. when all params have default values).
    /// </summary>
    public static IServiceProvider? Services { get; set; }

    /// <summary>Resolves a required service from the MCP DI container.</summary>
    public static T GetRequiredService<T>() where T : class
    {
        if (Services is null)
            throw new InvalidOperationException("McpGlobals.Services is not initialized");
        return Services.GetRequiredService<T>();
    }
}
