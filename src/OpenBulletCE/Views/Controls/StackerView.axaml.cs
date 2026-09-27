using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Extreme.Net;
using OpenBulletCE.ViewModels;
using OpenBulletCE.Views.Dialogs;
using RuriLib;
using RuriLib.LS;
using RuriLib.Models;
using RuriLib.Runner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenBulletCE.Views.Controls;

public partial class StackerView : UserControl
{
    private StackerViewModel _vm;
    private LoliScript _ls;
    private bool _debuggerRunning;
    private CancellationTokenSource _debuggerCts;
    private string _logFilter = "";
    private readonly List<LogEntry> _fullLog = new();

    public StackerView()
    {
        InitializeComponent();
        LoliScriptEditor.TextArea.TextView.LineTransformers.Add(new Editor.LoliScriptColorizer());
        LoliScriptEditor.WordWrap = OB.OBSettings?.Themes?.WordWrap ?? false;
        if (OB.OBSettings?.Themes != null)
            OB.OBSettings.Themes.PropertyChanged += (s, a) =>
            {
                if (a.PropertyName == nameof(OBSettingsThemes.WordWrap))
                    LoliScriptEditor.WordWrap = OB.OBSettings.Themes.WordWrap;
            };
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += OnDetached;
        HtmlView.WebViewCreated += (_, _) =>
        {
            _htmlViewReady = true;
            RenderHtml();
            try
            {
                // Debug view = static snapshot: cancel any in-page navigation to a
                // real URL (form submits, link clicks, JS redirects)
                if (((WebViewCore.IVirtualWebView)HtmlView).PlatformView is WebViewCore.IWebViewEventHandler eh)
                    eh.NavigationStarting += (_, a) =>
                    {
                        if (a is WebViewCore.Events.WebViewUrlLoadingEventArg arg &&
                            arg.Url is { Scheme: "http" or "https" })
                            arg.Cancel = true;
                    };
            }
            catch { }
        };
        // And never let links/popups spawn new windows or leave the view
        HtmlView.WebViewNewWindowRequested += (_, a) =>
            a.UrlLoadingStrategy = WebViewCore.Enums.UrlRequestStrategy.CancelLoad;
        // The platform webview is disposed/recreated on every tab switch —
        // re-push content shortly after re-attach in case init raced
        HtmlView.AttachedToVisualTree += async (_, _) =>
        {
            await Task.Delay(800);
            RenderHtml();
        };
    }

    // Persist the in-memory stack/script back into the config when leaving the view,
    // so unsaved work isn't lost when switching sub-tabs.
    private void OnDetached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_vm?.Config?.Config == null) return;
        _vm.Config.Config.Script = _vm.View == ViewModels.StackerView.Blocks
            ? string.Join("\n", _vm.GetList().Select(b => b.ToLS()))
            : LoliScriptEditor.Text ?? "";
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is StackerViewModel vm)
        {
            _vm = vm;
            _ls = new LoliScript(vm.Config?.Config?.Script ?? "");
            LoliScriptEditor.Text = _ls.Script;

            // Populate data types from the environment wordlist types
            if (DataTypeCombo != null && DataTypeCombo.Items.Count == 0)
            {
                foreach (var t in OB.Settings.Environment.WordlistTypes)
                    DataTypeCombo.Items.Add(new ComboBoxItem { Content = t.Name });
                if (DataTypeCombo.Items.Count > 0)
                    DataTypeCombo.SelectedIndex = 0;
            }

            // Sync debugger fields
            if (TestDataBox != null) TestDataBox.Text = vm.TestData;
            if (TestProxyBox != null) TestProxyBox.Text = vm.TestProxy;
            if (UseProxyCheck != null) UseProxyCheck.IsChecked = vm.UseProxy;
            if (SBSCheck != null) SBSCheck.IsChecked = vm.SBS;
            if (ProxyTypeCombo != null) ProxyTypeCombo.SelectedIndex = (int)vm.ProxyType;

            if (!OB.OBSettings.General.DisplayLoliScriptOnLoad)
                SwitchToStack();
        }
    }

    // --- View switching ---

    private void SwitchToLoliScript_Click(object? sender, RoutedEventArgs e) => SwitchToLoliScript();
    private void SwitchToStack_Click(object? sender, RoutedEventArgs e) => SwitchToStack();
    private void OpenDoc_Click(object? sender, RoutedEventArgs e) => _ = OpenDoc();

    private void SwitchToLoliScript()
    {
        if (_vm == null) return;
        _ls.FromBlocks(_vm.GetList());
        LoliScriptEditor.Text = _ls.Script;
        _vm.View = ViewModels.StackerView.LoliScript;
        LoliScriptPanel.IsVisible = true;
        StackPanel.IsVisible = false;
    }

    private void SwitchToStack()
    {
        if (_vm == null) return;

        List<BlockBase> blocks;
        try
        {
            _ls = new LoliScript(LoliScriptEditor.Text ?? "");
            blocks = _ls.ToBlocks();
        }
        catch (Exception ex)
        {
            _ = Alerter.Dialogs?.AlertAsync($"Error parsing LoliScript: {ex.Message}", "Syntax Error");
            return;
        }

        _vm.ClearBlocks();
        foreach (var block in blocks)
            _vm.AddBlock(block);

        _vm.CurrentBlock = null;
        BlockEditorHost.Content = null;
        _vm.View = ViewModels.StackerView.Blocks;
        LoliScriptPanel.IsVisible = false;
        StackPanel.IsVisible = true;
    }

    private async Task OpenDoc()
    {
        var doc = new DialogLSDoc();
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window w)
            await doc.ShowDialog(w);
        else
            doc.Show();
    }

    // --- Block management ---

    private async void AddBlock_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var dialog = new DialogAddBlock();
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window w)
        {
            var result = await dialog.ShowDialog<bool>(w);
            if (result && dialog.SelectedBlock != null)
            {
                // Insert right after the selected block (OB1 behavior), at the end if none
                var index = _vm.CurrentBlock != null ? _vm.CurrentBlockIndex + 1 : -1;
                _vm.AddBlock(dialog.SelectedBlock, index);
            }
        }
    }

    private void RemoveBlock_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var selected = _vm.SelectedBlocks.ToList();
        foreach (var b in selected)
        {
            _vm.LastDeletedBlock = b.Block;
            _vm.LastDeletedIndex = _vm.Stack.IndexOf(b);
            _vm.Stack.Remove(b);
        }
        _vm.UpdateHeights();
    }

    private void ToggleBlock_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        foreach (var b in _vm.SelectedBlocks)
            b.Disable();
    }

    private void CloneBlock_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var selected = _vm.SelectedBlocks.ToList();
        foreach (var b in selected)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(b.Block);
                var clone = Newtonsoft.Json.JsonConvert.DeserializeObject(json, b.Block.GetType()) as BlockBase;
                if (clone != null)
                    _vm.AddBlock(clone, _vm.Stack.IndexOf(b) + 1);
            }
            catch { }
        }
    }

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm?.CurrentBlock != null)
            _vm.MoveBlockUp(_vm.CurrentBlock);
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm?.CurrentBlock != null)
            _vm.MoveBlockDown(_vm.CurrentBlock);
    }

    private void Save_Click(object? sender, RoutedEventArgs e) => SaveConfig();

    public void SaveConfig()
    {
        if (_vm?.Config == null) return;
        try
        {
            if (_vm.View == ViewModels.StackerView.Blocks)
                _ls.FromBlocks(_vm.GetList());
            else
                _ls = new LoliScript(LoliScriptEditor.Text ?? "");

            _vm.Config.Config.Script = _ls.Script;
            OB.ConfigManager.CurrentConfig ??= _vm.Config;
            OB.ConfigManager.Update(_vm.Config);
            OB.Logger.LogInfo(Components.Stacker, $"Saved config {_vm.Config.Name}");
        }
        catch (Exception ex)
        {
            OB.Logger.LogError(Components.Stacker, $"Could not save config: {ex.Message}");
        }
    }

    // --- Block selection ---

    private void BlockList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_vm == null) return;
        _vm.DeselectAll();

        if (sender is ListBox listBox && listBox.SelectedItems != null)
        {
            foreach (var item in listBox.SelectedItems)
            {
                if (item is StackerBlockViewModel sb)
                    sb.Selected = true;
            }

            var selected = _vm.SelectedBlocks;
            if (selected.Count > 0)
            {
                _vm.CurrentBlock = selected[^1];
                BlockEditorHost.Content = StackerBlocks.BlockViewFactory.Create(_vm.CurrentBlock.Block);
                if (BlockLabelBox != null)
                {
                    BlockLabelBox.IsEnabled = true;
                    BlockLabelBox.Text = _vm.CurrentBlock.Block.Label;
                }
            }
            else
            {
                _vm.CurrentBlock = null;
                BlockEditorHost.Content = null;
                if (BlockLabelBox != null)
                {
                    BlockLabelBox.IsEnabled = false;
                    BlockLabelBox.Text = "";
                }
            }
        }
    }

    private void BlockLabel_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_vm?.CurrentBlock?.Block != null && BlockLabelBox != null)
            _vm.CurrentBlock.Block.Label = BlockLabelBox.Text ?? "";
    }

    private void ProxyTypeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_vm != null && ProxyTypeCombo?.SelectedItem is ComboBoxItem item
            && Enum.TryParse<Extreme.Net.ProxyType>(item.Content?.ToString(), out var pt))
            _vm.ProxyType = pt;
    }

    // --- Debugger ---

    private async void StartDebugger_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _debuggerRunning)
        {
            // Cancel
            _debuggerCts?.Cancel();
            return;
        }

        // Persist debugger inputs to the VM
        _vm.TestData = TestDataBox?.Text ?? "";
        _vm.TestProxy = TestProxyBox?.Text ?? "";
        _vm.UseProxy = UseProxyCheck?.IsChecked == true;
        _vm.SBS = SBSCheck?.IsChecked == true;
        _vm.TestDataType = (DataTypeCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Default";

        _debuggerRunning = true;
        _debuggerCts = new CancellationTokenSource();
        var ct = _debuggerCts.Token;

        try
        {
            // Dispose previous browser
            if (_vm.BotData?.BrowserOpen == true)
            {
                try { _vm.BotData.Driver?.Quit(); } catch { }
            }

            // Build proxy
            CProxy proxy = null;
            var proxyText = TestProxyBox?.Text ?? "";
            if (!string.IsNullOrWhiteSpace(proxyText))
            {
                try { proxy = new CProxy().Parse(proxyText, _vm.ProxyType); }
                catch { proxy = new CProxy(proxyText, _vm.ProxyType); }
            }

            // Build data
            var dataTypeName = (DataTypeCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Default";
            var dataType = OB.Settings.Environment.GetWordlistType(dataTypeName);
            var cData = new CData(TestDataBox?.Text ?? "", dataType);

            _vm.BotData = new BotData(
                OB.Settings.RLSettings,
                _vm.Config.Config.Settings,
                cData,
                proxy,
                UseProxyCheck?.IsChecked == true,
                new Random());

            // Set script
            var script = _vm.View == ViewModels.StackerView.Blocks
                ? _vm.GetList().Select(b => b.ToLS()).Aggregate((a, b) => a + "\n" + b)
                : LoliScriptEditor.Text ?? "";

            _ls = new LoliScript(script);
            _ls.Reset();

            // TakeStep() clears BotData.LogBuffer every block — keep a cumulative
            // log like classic OB and drain the per-block buffer after each step.
            lock (_fullLog) _fullLog.Clear();
            _fullLog.Add(new LogEntry(
                $"===== DEBUGGER STARTED FOR {_vm.Config.Name} =====",
                RuriLib.Models.Colors.White));
            ApplyLogFilter();

            var sw = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                while (_ls.CanProceed && !ct.IsCancellationRequested)
                {
                    try
                    {
                        _ls.TakeStep(_vm.BotData);
                    }
                    catch (Exception ex)
                    {
                        _vm.BotData.LogBuffer.Add(new LogEntry(
                            $"Error on {_ls.CurrentLine}: {ex.Message}",
                            RuriLib.Models.Colors.Tomato));
                    }

                    lock (_fullLog) _fullLog.AddRange(_vm.BotData.LogBuffer);
                    Dispatcher.UIThread.Post(ApplyLogFilter);
                }
            }, ct);

            sw.Stop();

            // Print results on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                lock (_fullLog)
                    _fullLog.Add(new LogEntry(
                        $"===== DEBUGGER ENDED ({sw.ElapsedMilliseconds / 1000.0}s) STATUS: {_vm.BotData.StatusString} =====",
                        StatusColor(_vm.BotData.Status)));
                RefreshOutputs();
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DataOutput.ItemsSource = new[] { new LogEntry($"Debugger error: {ex.Message}", RuriLib.Models.Colors.Tomato) };
            });
        }
        finally
        {
            _debuggerRunning = false;
        }
    }

    /// <summary>Colors the bot status like classic OB (success=green, fail/error=red, ban/retry=yellow).</summary>
    private static RuriLib.Models.Color StatusColor(BotStatus s) => s switch
    {
        BotStatus.SUCCESS => RuriLib.Models.Colors.LimeGreen,
        BotStatus.FAIL => RuriLib.Models.Colors.OrangeRed,
        BotStatus.ERROR => RuriLib.Models.Colors.Tomato,
        BotStatus.BAN => RuriLib.Models.Colors.Gold,
        BotStatus.RETRY => RuriLib.Models.Colors.Yellow,
        BotStatus.CUSTOM => RuriLib.Models.Colors.Aqua,
        _ => RuriLib.Models.Colors.White
    };

    /// <summary>Rebuilds the colored Data + Log tabs from the current BotData.</summary>
    private void RefreshOutputs()
    {
        var data = _vm!.BotData!;
        var entries = new List<LogEntry>
        {
            new($"BOT STATUS: {data.StatusString}", StatusColor(data.Status)),
            new("", RuriLib.Models.Colors.White),
            new("VARIABLES:", RuriLib.Models.Colors.Cyan)
        };
        foreach (var v in data.Variables.All.Where(v => !v.Hidden))
            entries.Add(new LogEntry($"{v.Name} ({v.Type}) = {v.Value}",
                v.IsCapture ? RuriLib.Models.Colors.Tomato : RuriLib.Models.Colors.Gold));
        DataOutput.ItemsSource = entries;

        _lastHtml = data.ResponseSource ?? "";
        if (HtmlSource != null)
            HtmlSource.Text = string.IsNullOrEmpty(_lastHtml)
                ? "(no response source yet — run a REQUEST block)"
                : _lastHtml;
        RenderHtml();

        ApplyLogFilter();
    }

    private string _lastHtml = "";
    private bool _htmlViewReady;

    private void RenderHtml()
    {
        if (HtmlView == null) return;
        try
        {
            var html = string.IsNullOrEmpty(_lastHtml)
                ? "<html><body style='background:#14161d;color:#888;font-family:sans-serif;padding:16px'>(no response source yet — run a REQUEST block)</body></html>"
                : _lastHtml;
            // Keep it a snapshot: swallow form submits + link clicks inside the page
            const string freezeJs = "<script>document.addEventListener('submit',function(e){e.preventDefault()},true);document.addEventListener('click',function(e){var a=e.target&&e.target.closest?e.target.closest('a'):null;if(a)e.preventDefault()},true);</script>";
            if (html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
                html = System.Text.RegularExpressions.Regex.Replace(html, "</body>", freezeJs + "</body>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            else
                html += freezeJs;
            // Avalonia dedupes identical StyledProperty values, so force a change
            // to guarantee the NavigateToString call fires every run
            if (HtmlView.HtmlContent == html)
                HtmlView.HtmlContent = "\u200b"; // zero-width space sentinel
            HtmlView.HtmlContent = html;
        }
        catch { }
    }

    private void HtmlSourceToggle_Click(object? sender, RoutedEventArgs e)
    {
        var showingSource = HtmlSource?.IsVisible == true;
        if (HtmlSource != null) HtmlSource.IsVisible = !showingSource;
        if (HtmlView != null) HtmlView.IsVisible = showingSource;
        if (HtmlSourceToggle != null) HtmlSourceToggle.Content = showingSource ? "Source" : "Rendered";
    }

    private async void CopyHtml_Click(object? sender, RoutedEventArgs e)
    {
        var text = _lastHtml;
        if (!string.IsNullOrEmpty(text))
            await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask);
    }

    private void OpenHtml_Click(object? sender, RoutedEventArgs e)
    {
        var html = _vm?.BotData?.ResponseSource;
        if (string.IsNullOrEmpty(html)) return;
        try
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "obce-response.html");
            System.IO.File.WriteAllText(path, html);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            OB.Logger.LogError(Components.Stacker, $"Could not open HTML: {ex.Message}");
        }
    }

    private void DbgTab_Checked(object? sender, RoutedEventArgs e)
    {
        if (DataOutput == null || LogPanel == null || HtmlPanel == null) return;
        DataOutput.IsVisible = sender == DbgTabData;
        LogPanel.IsVisible = sender == DbgTabLog;
        HtmlPanel.IsVisible = sender == DbgTabHtml;
        if (sender == DbgTabHtml) RenderHtml();
    }

    private void LogSearch_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        _logFilter = LogSearch.Text?.Trim() ?? "";
        ApplyLogFilter();
    }

    private void ApplyLogFilter()
    {
        List<LogEntry> snapshot;
        lock (_fullLog) snapshot = _fullLog.ToList();
        var items = string.IsNullOrEmpty(_logFilter)
            ? snapshot
            : snapshot.Where(l => l.LogString.Contains(_logFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        LogOutput.ItemsSource = items;
        if (items.Count > 0)
            LogOutput.ScrollIntoView(items[^1]);
    }

    private async void CopyLog_Click(object? sender, RoutedEventArgs e)
    {
        List<LogEntry> snapshot;
        lock (_fullLog) snapshot = _fullLog.ToList();
        if (snapshot.Count == 0) return;
        var text = string.Join("\n", snapshot.Select(l => l.LogString));
        await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask);
    }

    private async void CopyData_Click(object? sender, RoutedEventArgs e)
    {
        if (DataOutput.ItemsSource is IEnumerable<LogEntry> entries)
        {
            var text = string.Join("\n", entries.Select(l => l.LogString));
            await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask);
        }
    }

    private void NextStep_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm?.BotData != null && _ls != null && _ls.CanProceed)
        {
            try
            {
                _ls.TakeStep(_vm.BotData);
                lock (_fullLog) _fullLog.AddRange(_vm.BotData.LogBuffer);
                RefreshOutputs();
            }
            catch (Exception ex)
            {
                DataOutput.ItemsSource = new[] { new LogEntry($"Step error: {ex.Message}", RuriLib.Models.Colors.Tomato) };
            }
        }
    }
}
