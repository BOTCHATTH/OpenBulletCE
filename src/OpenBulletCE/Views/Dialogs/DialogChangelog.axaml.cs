using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.Generic;

namespace OpenBulletCE.Views.Dialogs;

public record ChangelogSection(string Title, string[] Items);
public record ChangelogRelease(string Version, string Date, string Blurb, ChangelogSection[] Sections);

public partial class DialogChangelog : Window
{
    public static readonly List<ChangelogRelease> Releases = new()
    {
        new("1.8.9.1", "2026-09-27",
            "This patch adds a rendered HTML debugger view, a real Script block, and a stack of UI fixes and polish.",
            new ChangelogSection[]
            {
                new("Debugger and Stacker", new[]
                {
                    "New HTML tab renders the last response in an embedded WebView2 view — like the original HTML view",
                    "Source toggle, Copy and Open in Browser buttons on the HTML tab",
                    "Frozen snapshot mode — form submits, link clicks and redirects inside the preview can't navigate away",
                    "New BLOCK:Script block — Jint, NodeJS and IronPython interpreters, multiple typed outputs",
                    "Add Block now inserts after the selected block instead of always appending at the end"
                }),
                new("Runner", new[]
                {
                    "Clone button on each runner — copies config, list, bots, proxies and start point",
                    "Log lines and Hits/Bots grids now colored by status (HIT green, CUSTOM orange, TOCHECK aqua...)",
                    "Data column shows a short 8-char name; full path stays in the tooltip and copy actions",
                    "Horizontal scrollbars always visible; Capture column widened; full-text cell tooltips",
                    "Data pool list names capped with ellipsis + tooltip instead of overflowing the card"
                }),
                new("Fixes and Platform", new[]
                {
                    "Switching configs no longer wipes the selected cookielist/wordlist",
                    "Cookie folders added blank now auto-derive their name from the folder path",
                    "Fixed the title bar drifting off-position when the window is maximized",
                    "Version shown in the title bar and on the Home page; changelog added (you're reading it)",
                    "Built-in ambient wallpaper when no custom background image is set",
                    "Added Windows app.manifest so native hosts (WebView2) can attach"
                })
            }),

        new("1.8.9", "2026-09-27",
            "First public release — the full Cookie Edition ported to Avalonia and .NET 8, running on Windows and Linux.",
            new ChangelogSection[]
            {
                new("Core", new[]
                {
                    "Complete loliscript engine port with all classic blocks",
                    "Runner with parallel bots, breakpoints, custom statuses and live stats",
                    "Stacker with block view, syntax-highlighted loliscript view and step debugger",
                    "Hits DB, proxies with testing, wordlists, cookielists, config manager"
                }),
                new("Automation", new[]
                {
                    "Embedded MCP server — drive the app from AI agents",
                    "CurlTls plugin — browser-grade TLS fingerprinting built in",
                    "IronPython and Jint scripting inside configs"
                }),
                new("Cross-platform", new[]
                {
                    "Windows x64 and Linux x64 self-contained builds — no .NET install needed",
                    "Archive cookielists — drop .zip / .rar / .7z / .tar / .gz and it extracts on the fly"
                })
            })
    };

    public DialogChangelog()
    {
        InitializeComponent();
        BuildContent();
        Opened += (_, _) =>
        {
            // keep the whole window inside the visible screen area
            if (Screens.ScreenFromVisual(this) is { } scr && Height > scr.WorkingArea.Height - 60)
                Height = scr.WorkingArea.Height - 60;
        };
    }

    private void BuildContent()
    {
        var textPrimary = (IBrush?)Application.Current?.FindResource("BrushTextPrimary") ?? Brushes.White;
        var textSecondary = (IBrush?)Application.Current?.FindResource("BrushTextSecondary") ?? Brushes.Gray;
        var accent = (IBrush?)Application.Current?.FindResource("BrushAccent") ?? Brushes.DodgerBlue;

        foreach (var rel in Releases)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 28) };

            panel.Children.Add(new TextBlock
            {
                Text = $"{rel.Version} ({rel.Date})",
                FontSize = 22, FontWeight = FontWeight.Bold,
                Foreground = textPrimary
            });

            if (!string.IsNullOrEmpty(rel.Blurb))
                panel.Children.Add(new TextBlock
                {
                    Text = rel.Blurb, FontSize = 13,
                    Foreground = textSecondary, TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 10)
                });

            foreach (var sec in rel.Sections)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = sec.Title, FontSize = 13, FontWeight = FontWeight.SemiBold,
                    Foreground = textPrimary, Margin = new Thickness(0, 0, 0, 4)
                });

                foreach (var item in sec.Items)
                    panel.Children.Add(new SelectableTextBlock
                    {
                        Text = "•  " + item, FontSize = 13,
                        Foreground = textSecondary, TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20, 2, 0, 2)
                    });
            }

            Body.Children.Add(panel);
        }
    }
}
