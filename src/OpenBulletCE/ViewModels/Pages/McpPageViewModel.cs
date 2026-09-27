using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenBulletCE.Mcp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBulletCE.ViewModels.Pages;

public partial class McpPageViewModel : ViewModelBase
{
    public OBSettingsMcp Settings => OB.OBSettings.Mcp;

    [ObservableProperty]
    private string _statusText = "Stopped";

    public Avalonia.Media.IBrush StatusBrush => StatusText switch
    {
        "Running" => Avalonia.Media.Brush.Parse("#ADFF2F"),
        "Stopped" => Avalonia.Media.Brush.Parse("#9BA6B8"),
        _ => Avalonia.Media.Brush.Parse("#FF6347")
    };

    [ObservableProperty]
    private string _lastError = "";

    [ObservableProperty]
    private string _endpointUrl = "";

    [ObservableProperty]
    private ObservableCollection<AgentInstallItem> _agents = new();

    public bool CanStart => StatusText != "Running";
    public bool CanStop => StatusText == "Running";

    public McpPageViewModel()
    {
        RefreshAgents();
        RefreshStatus();

        // Poll server status (it runs on a background thread)
        var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => RefreshStatus();
        timer.Start();
    }

    private void RefreshStatus()
    {
        StatusText = McpServerHost.Status.ToString();
        EndpointUrl = McpServerHost.EndpointUrl ?? "disabled";
        OnPropertyChanged(nameof(StatusBrush));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
    }

    [RelayCommand]
    private async Task ReloadServer()
    {
        Settings.SaveToDisk();
        await McpServerHost.StopAsync();
        await McpServerHost.StartAsync();
        RefreshStatus();
    }

    [RelayCommand]
    private async Task StartServer()
    {
        Settings.Enabled = true;
        Settings.SaveToDisk();
        await McpServerHost.StartAsync();
        RefreshStatus();
    }

    [RelayCommand]
    private async Task StopServer()
    {
        await McpServerHost.StopAsync();
        RefreshStatus();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        Settings.SaveToDisk();
        OB.Logger.LogInfo(Components.Main, "MCP settings saved (Reload to apply)");
    }

    [RelayCommand]
    private void RefreshAgents()
    {
        Agents.Clear();
        foreach (var a in McpAgentInstaller.Agents)
            Agents.Add(new AgentInstallItem(a));
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var a in Agents) a.IsSelected = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var a in Agents) a.IsSelected = false;
    }

    [RelayCommand]
    private async Task InstallAgent(AgentInstallItem item)
    {
        if (item == null) return;
        var key = string.IsNullOrEmpty(Settings.ApiKey) ? null : Settings.ApiKey;
        var r = await McpAgentInstaller.InstallForAgent(item.AgentId, McpServerHost.EndpointUrl ?? "", key);
        if (r.Success)
        {
            item.IsInstalled = true;
            OB.Logger.LogInfo(Components.Main, $"MCP installed for {item.DisplayName}");
        }
        else
            OB.Logger.LogError(Components.Main, $"MCP install failed for {item.DisplayName}: {r.Error}", true);
    }

    [RelayCommand]
    private async Task UninstallAgent(AgentInstallItem item)
    {
        if (item == null) return;
        var r = await McpAgentInstaller.UninstallForAgent(item.AgentId);
        if (r.Success) item.IsInstalled = false;
    }

    [RelayCommand]
    private async Task InstallSelected()
    {
        var key = string.IsNullOrEmpty(Settings.ApiKey) ? null : Settings.ApiKey;
        foreach (var a in Agents.Where(x => x.IsSelected && !x.IsInstalled).ToList())
        {
            var r = await McpAgentInstaller.InstallForAgent(a.AgentId, McpServerHost.EndpointUrl ?? "", key);
            if (r.Success) a.IsInstalled = true;
        }
        OB.Logger.LogInfo(Components.Main, "MCP install finished for selected agents");
    }
}

public partial class AgentInstallItem : ViewModelBase
{
    public string AgentId { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string ConfigPath { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isInstalled;

    public AgentInstallItem(McpAgentInstaller.AgentInfo agent)
    {
        AgentId = agent.Id;
        DisplayName = agent.DisplayName;
        Description = agent.Description;
        ConfigPath = agent.ConfigPathResolver();
        _isInstalled = McpAgentInstaller.IsInstalled(agent.Id);
    }
}
