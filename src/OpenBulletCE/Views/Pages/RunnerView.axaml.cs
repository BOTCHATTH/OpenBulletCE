using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OpenBulletCE.ViewModels;
using OpenBulletCE.ViewModels.Pages;
using OpenBulletCE.Views.Dialogs;
using RuriLib;
using RuriLib.Enums;
using RuriLib.Models;
using RuriLib.Runner;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBulletCE.Views.Pages;

public partial class RunnerView : UserControl
{
    public RunnerView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HookLogLines();
        DetachedFromVisualTree += (_, _) => UnhookLogLines();
    }

    private RunnerPageViewModel _logHookedVm;

    private void HookLogLines()
    {
        if (DataContext is RunnerPageViewModel vm && _logHookedVm != vm)
        {
            UnhookLogLines();
            _logHookedVm = vm;
            vm.LogLines.CollectionChanged += LogLines_CollectionChanged;
        }
    }

    private void UnhookLogLines()
    {
        if (_logHookedVm != null)
        {
            _logHookedVm.LogLines.CollectionChanged -= LogLines_CollectionChanged;
            _logHookedVm = null;
        }
    }

    private bool _scrollQueued;

    private void LogLines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Batch flushes add many lines at once — queue a single scroll per burst.
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && !_scrollQueued)
        {
            _scrollQueued = true;
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () =>
                {
                    _scrollQueued = false;
                    RunnerLogScroll?.ScrollToEnd();
                },
                Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void RunnerLogCopy_Click(object sender, RoutedEventArgs e)
    {
        if (Page == null) return;
        var text = string.Join(Environment.NewLine,
            Page.LogLines.Select(l => $"[{l.Time}] {l.Text}"));
        CopyToClipboard(text);
    }

    private Window GetParentWindow()
        => TopLevel.GetTopLevel(this) as Window;

    private RunnerPageViewModel Page => DataContext as RunnerPageViewModel;

    private RunnerInstance GetRunner(object sender)
        => (sender as Control)?.Tag as RunnerInstance;

    // ─── Manager list ───

    private void RunnerItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Ignore clicks that landed on buttons inside the item
        var src = e.Source as Control;
        while (src != null && src != sender)
        {
            if (src is Button) return;
            src = src.Parent as Control;
        }

        var runner = (sender as Control)?.Tag as RunnerInstance;
        if (runner != null && Page != null)
        {
            Page.SelectedRunner = runner;
            SyncProxyRadios(runner.ViewModel.ProxyMode);
            HookStatus(runner.ViewModel);
        }
    }

    private void SyncProxyRadios(ProxyMode mode)
    {
        ProxyDefRadio.IsChecked = mode == ProxyMode.Default;
        ProxyOnRadio.IsChecked = mode == ProxyMode.On;
        ProxyOffRadio.IsChecked = mode == ProxyMode.Off;
    }

    private RunnerViewModel _subscribedVm;

    private void HookStatus(RunnerViewModel vm)
    {
        if (_subscribedVm == vm) return;
        if (_subscribedVm != null)
            _subscribedVm.PropertyChanged -= Vm_PropertyChanged;
        _subscribedVm = vm;
        if (vm != null)
            vm.PropertyChanged += Vm_PropertyChanged;
        SyncStartButton(vm?.WorkerStatus);
    }

    private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "WorkerStatus")
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () => SyncStartButton((sender as RunnerViewModel)?.WorkerStatus));
    }

    private void SyncStartButton(WorkerStatus? status)
    {
        if (StartStopButton == null) return;
        StartStopButton.Classes.Clear();
        StartStopButton.Classes.Add(status == WorkerStatus.Running ? "Danger" : "Success");
    }

    // ─── Detail view ───

    private void ProxyRadio_Checked(object? sender, RoutedEventArgs e)
    {
        var rb = sender as RadioButton;
        if (rb?.Tag is string tag && Enum.TryParse<ProxyMode>(tag, out var mode)
            && Page?.SelectedRunner != null)
            Page.SelectedRunner.ViewModel.ProxyMode = mode;
    }

    private async void SelectConfig_Click(object sender, RoutedEventArgs e)
    {
        var runner = GetRunner(sender) ?? Page?.SelectedRunner;
        var window = GetParentWindow();
        if (runner == null || window == null) return;

        var dialog = new DialogSelectConfig();
        var result = await dialog.ShowDialog<bool>(window);

        if (result && dialog.SelectedConfig != null)
        {
            var vm = runner.ViewModel;
            vm.SetConfig(dialog.SelectedConfig.Config, OB.OBSettings.General.RecommendedBots);

            // Only drop the list the new config can't use — keep the compatible one
            if (vm.Config.Settings.Type == ConfigType.CookieEdition)
            {
                if (vm.Wordlist != null) vm.SetWordlist(null);
                RetrieveRecordCookie(runner);
            }
            else
            {
                if (vm.Cookielist != null) vm.SetCookielist(null);
                RetrieveRecord(runner);
            }
        }
    }

    private async void SelectList_Click(object sender, RoutedEventArgs e)
    {
        var runner = GetRunner(sender) ?? Page?.SelectedRunner;
        var window = GetParentWindow();
        if (runner == null || window == null) return;

        var vm = runner.ViewModel;

        if (vm.Config == null)
        {
            await Alerter.Dialogs.AlertAsync("Select a config first!", "Warning");
            return;
        }

        switch (vm.Config.Settings.Type)
        {
            case ConfigType.Default:
                var wlDialog = new DialogSelectWordlist();
                if (await wlDialog.ShowDialog<bool>(window) && wlDialog.SelectedWordlist != null)
                {
                    vm.SetWordlist(wlDialog.SelectedWordlist);
                    RetrieveRecord(runner);
                }
                break;

            case ConfigType.CookieEdition:
                var cookieDialog = new DialogSelectCookie();
                if (await cookieDialog.ShowDialog<bool>(window) && cookieDialog.SelectedCookie != null)
                {
                    vm.SetCookielist(cookieDialog.SelectedCookie);
                    RetrieveRecordCookie(runner);
                }
                break;
        }
    }

    private void RetrieveRecord(RunnerInstance runner)
    {
        if (runner.ViewModel.Config != null && runner.ViewModel.Wordlist != null)
            runner.ViewModel.StartingPoint = OB.RunnerManager.RetrieveRecord(runner.ViewModel.Config, runner.ViewModel.Wordlist);
    }

    private void RetrieveRecordCookie(RunnerInstance runner)
    {
        if (runner.ViewModel.Config != null && runner.ViewModel.Cookielist != null)
            runner.ViewModel.StartingPoint = OB.RunnerManager.RetrieveRecord(runner.ViewModel.Config, runner.ViewModel.Cookielist);
    }

    private void ShowLog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DialogShowLog();
        dialog.Show(GetParentWindow());
    }

    private void StartRunner_Click(object sender, RoutedEventArgs e)
    {
        var vm = Page?.SelectedRunner?.ViewModel;
        if (vm == null) return;

        if (vm.WorkerStatus == WorkerStatus.Running)
            vm.Stop();
        else
            vm.Start();
    }

    private async void JobOptions_Click(object sender, RoutedEventArgs e)
    {
        var vm = Page?.SelectedRunner?.ViewModel;
        var window = GetParentWindow();
        if (vm == null || window == null) return;

        var dialog = new DialogJobOptions(vm.RLSettings);
        var ok = await dialog.ShowDialog<bool>(window);

        if (ok && dialog.DelayedStartMinutes > 0 && vm.WorkerStatus != WorkerStatus.Running)
        {
            var minutes = dialog.DelayedStartMinutes;
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(minutes));
                if (vm.WorkerStatus != WorkerStatus.Running)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.Start());
            });
        }
    }

    // ─── Results filters ───

    private void HitsFilter_Click(object sender, RoutedEventArgs e)
    {
        if (Page?.SelectedRunner != null)
            Page.SelectedRunner.ViewModel.ResultsFilter = BotStatus.SUCCESS;
    }

    private void CustomFilter_Click(object sender, RoutedEventArgs e)
    {
        if (Page?.SelectedRunner != null)
            Page.SelectedRunner.ViewModel.ResultsFilter = BotStatus.CUSTOM;
    }

    private void ToCheckFilter_Click(object sender, RoutedEventArgs e)
    {
        if (Page?.SelectedRunner != null)
            Page.SelectedRunner.ViewModel.ResultsFilter = BotStatus.NONE;
    }

    // ─── Hits context menu ───

    private void CopySelectedData_Click(object sender, RoutedEventArgs e)
    {
        if (HitsGrid?.SelectedItems == null) return;

        var text = string.Join(Environment.NewLine,
            HitsGrid.SelectedItems.Cast<ValidData>().Select(v => v.Data));
        CopyToClipboard(text);
    }

    private void CopySelectedProxy_Click(object sender, RoutedEventArgs e)
    {
        if (HitsGrid?.SelectedItem is ValidData hit)
            CopyToClipboard(hit.Proxy ?? "");
    }

    private void CopySelectedCapture_Click(object sender, RoutedEventArgs e)
    {
        if (HitsGrid?.SelectedItems == null) return;

        var text = string.Join(Environment.NewLine,
            HitsGrid.SelectedItems.Cast<ValidData>().Select(v => $"{v.Data} | {v.CapturedData}"));
        CopyToClipboard(text);
    }

    private void SelectAllHits_Click(object sender, RoutedEventArgs e)
    {
        HitsGrid?.SelectAll();
    }

    private static string FormatHitLine(ValidData v)
        => string.IsNullOrEmpty(v.CapturedData) ? v.Data : $"{v.Data} | {v.CapturedData}";

    private async void SaveSelectedHits_Click(object sender, RoutedEventArgs e)
    {
        if (HitsGrid?.SelectedItems == null || HitsGrid.SelectedItems.Count == 0) return;
        var lines = HitsGrid.SelectedItems.Cast<ValidData>().Select(FormatHitLine);
        await SaveLinesToFile(lines);
    }

    private async void SaveAllHits_Click(object sender, RoutedEventArgs e)
    {
        var vm = Page?.SelectedRunner?.ViewModel;
        if (vm == null) return;
        await SaveLinesToFile(vm.FilteredResults.Select(FormatHitLine));
    }

    private async Task SaveLinesToFile(IEnumerable<string> lines)
    {
        var window = GetParentWindow();
        if (window == null) return;

        var file = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Save Hits",
            SuggestedFileName = $"hits_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            DefaultExtension = "txt"
        });
        if (file == null) return;

        var list = lines.ToList();
        await using var stream = await file.OpenWriteAsync();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(string.Join(Environment.NewLine, list));
        OB.Logger.LogInfo(Components.Runner, $"Saved {list.Count} hit(s) to {file.Name}");
    }

    private void ShowHitLog_Click(object sender, RoutedEventArgs e)
    {
        if (HitsGrid?.SelectedItem is not ValidData hit) return;

        var dialog = new DialogShowLog();
        dialog.Show(GetParentWindow());
    }

    private void SendToDebugger_Click(object sender, RoutedEventArgs e)
    {
        if (HitsGrid?.SelectedItem is not ValidData hit) return;

        try
        {
            var stacker = OB.Stacker;
            stacker.TestData = hit.Data;
            stacker.TestProxy = hit.Proxy;
            stacker.ProxyType = hit.ProxyType;
            OB.Logger.LogInfo(Components.Runner, "Sent to the debugger");
        }
        catch (Exception ex) { OB.Logger.LogError(Components.Runner, $"Could not send to debugger: {ex.Message}"); }
    }

    private void ShowHTML_Click(object sender, RoutedEventArgs e)
    {
        if (HitsGrid?.SelectedItem is not ValidData hit) return;

        try
        {
            File.WriteAllText("source.html", hit.Source ?? "");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("source.html") { UseShellExecute = true });
            OB.Logger.LogInfo(Components.Runner, "Saved HTML to source.html");
        }
        catch (Exception ex) { OB.Logger.LogError(Components.Runner, $"Couldn't show HTML: {ex.Message}"); }
    }

    private void CopyToClipboard(string text)
    {
        try
        {
            var clipboard = GetParentWindow()?.Clipboard;
            clipboard?.SetTextAsync(text);
        }
        catch { }
    }
}
