using Avalonia.Controls;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenBulletCE.Services;

/// <summary>
/// Checks GitHub releases once per launch and pops a dialog when a newer
/// version than OB.Version is published.
/// </summary>
public static class UpdateChecker
{
    private const string Repo = "BOTCHATTH/OpenBulletCE";
    private static bool _checked;

    public static async Task CheckAsync(Window parent)
    {
        if (_checked) return;
        _checked = true;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var req = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{Repo}/releases/latest");
            req.Headers.UserAgent.ParseAdd($"OpenBulletCE/{OB.Version}");
            req.Headers.Accept.ParseAdd("application/vnd.github+json");

            var res = await http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return;

            var json = JObject.Parse(await res.Content.ReadAsStringAsync());
            var tag = json["tag_name"]?.ToString()?.TrimStart('v', 'V');
            var url = json["html_url"]?.ToString();
            if (string.IsNullOrEmpty(tag)) return;
            if (!Version.TryParse(tag, out var latest)) return;
            if (!Version.TryParse(OB.Version, out var current)) return;
            if (latest <= current) return;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new ConfirmDialog(
                    $"A new version is out: v{tag} (you're on {OB.Version}).\n\nOpen the download page?",
                    "Update available");
                if (await dialog.ShowDialog<bool>(parent) && !string.IsNullOrEmpty(url))
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { }
                }
            });
        }
        catch { }
    }
}
