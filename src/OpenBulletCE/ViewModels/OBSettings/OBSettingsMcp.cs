namespace OpenBulletCE.ViewModels;

/// <summary>Settings for the embedded MCP (Model Context Protocol) server.</summary>
public class OBSettingsMcp : ViewModelBase
{
    private bool _enabled = true;
    public bool Enabled { get => _enabled; set { _enabled = value; OnPropertyChanged(); } }

    private int _port = 5115;
    public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }

    private string _bindAddress = "127.0.0.1";
    public string BindAddress { get => _bindAddress; set { _bindAddress = value; OnPropertyChanged(); } }

    private string _apiKey = "";
    /// <summary>Optional bearer token required on the /mcp endpoint. Empty = no auth.</summary>
    public string ApiKey { get => _apiKey; set { _apiKey = value; OnPropertyChanged(); } }

    /// <summary>Persists the whole OBSettings (including this section) to disk.</summary>
    public void SaveToDisk() => OBIOManager.SaveSettings(OB.obSettingsFile, OB.OBSettings);

    public void Reset()
    {
        Enabled = true;
        Port = 5115;
        BindAddress = "127.0.0.1";
        ApiKey = "";
    }
}
