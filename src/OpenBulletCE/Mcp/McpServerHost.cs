using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuriLib;

namespace OpenBulletCE.Mcp;

/// <summary>
/// Manages the embedded MCP (Model Context Protocol) server lifecycle.
/// Hosts a Kestrel WebApplication serving the /mcp streamable-http endpoint
/// inside the OpenBullet CE native process, same as OpenBullet2.Native.
/// </summary>
public static class McpServerHost
{
    private static WebApplication? _app;
    private static readonly object _lock = new();

    public static McpServerStatus Status { get; private set; } = McpServerStatus.Stopped;

    public static string? EndpointUrl
    {
        get
        {
            var bind = BindAddress;
            if (string.IsNullOrWhiteSpace(bind)) return null;
            return $"http://{bind}:{Port}/mcp";
        }
    }

    public static int Port => OB.OBSettings?.Mcp?.Port ?? 5115;
    public static string BindAddress => OB.OBSettings?.Mcp?.BindAddress ?? "127.0.0.1";
    public static string ApiKey => OB.OBSettings?.Mcp?.ApiKey ?? "";

    public enum McpServerStatus { Stopped, Running, Error }

    /// <summary>Starts the MCP server if enabled.</summary>
    public static async Task StartAsync()
    {
        if (OB.OBSettings?.Mcp?.Enabled != true)
        {
            OB.Logger.Log(Components.Main, RuriLib.LogLevel.Info, "MCP server is disabled in settings");
            return;
        }
        await RestartAsync();
    }

    /// <summary>Restarts the MCP server. Called on settings change or app init.</summary>
    public static async Task RestartAsync()
    {
        try
        {
            await StopAsync();

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseKestrel(options =>
            {
                options.Limits.MaxConcurrentConnections = 200;
                options.Limits.MaxRequestBodySize = 4 * 1024 * 1024;
                options.Listen(System.Net.IPAddress.Parse(NormalizeBindAddress(BindAddress)), Port);
            });

            // Suppress console output — we are inside a desktop app
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new McpLoggerProvider());
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

            builder.Services.AddMcpServer(options =>
                {
                    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                    {
                        Name = "OC2",
                        Version = McpManifest.Version
                    };
                })
                .WithHttpTransport(options =>
                {
                    // Stateless mode required — MCP tools run on different
                    // HTTP requests and have no access to HttpContext
                    options.Stateless = true;
                })
                .WithToolsFromAssembly();

            _app = builder.Build();
            McpGlobals.Services = _app.Services;

            // Optional API key auth via ?api_key= or Authorization: Bearer header
            _app.Use(async (context, next) =>
            {
                if (!string.IsNullOrEmpty(ApiKey) && context.Request.Path.StartsWithSegments("/mcp"))
                {
                    var apiKey = (string?)context.Request.Query["api_key"]
                        ?? ((string?)context.Request.Headers["Authorization"])?.Replace("Bearer ", "");

                    if (apiKey != ApiKey)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unauthorized: invalid API key");
                        return;
                    }
                }
                await next();
            });

            // Friendly root endpoint for browser hits
            _app.MapGet("/", () => new
            {
                app = "OC2",
                message = "OC2 — OpenBullet Cookie 2 MCP Server",
                endpoint = "/mcp",
                status = "running"
            });

            _app.MapMcp("/mcp");

            lock (_lock) Status = McpServerStatus.Running;
            OB.Logger.Log(Components.Main, RuriLib.LogLevel.Info, $"MCP server started on {EndpointUrl}");
            await _app.RunAsync();
        }
        catch (Exception ex)
        {
            lock (_lock) Status = McpServerStatus.Error;
            OB.Logger.Log(Components.Main, RuriLib.LogLevel.Error, $"MCP server failed: {ex.Message}");
        }
    }

    /// <summary>Gracefully stops the MCP server.</summary>
    public static async Task StopAsync()
    {
        if (_app != null)
        {
            try { await _app.StopAsync(new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3)).Token); } catch { }
            try { await _app.DisposeAsync(); } catch { }
            _app = null;
            McpGlobals.Services = null;
        }
        lock (_lock) Status = McpServerStatus.Stopped;
    }

    private static string NormalizeBindAddress(string bindAddress)
    {
        var trimmed = bindAddress.Trim();
        return trimmed switch
        {
            "localhost" => "127.0.0.1",
            "*" or "+" or "0.0.0.0" => "0.0.0.0",
            _ => trimmed
        };
    }
}
