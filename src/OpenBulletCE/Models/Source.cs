using Newtonsoft.Json;
using RuriLib.ViewModels;

namespace OpenBulletCE.Models;

public class Source : ViewModelBase
{
    public enum AuthMode
    {
        ApiKey,
        UserPass
    }

    public int Id { get; set; }

    private string _apiUrl = "";
    public string ApiUrl { get => _apiUrl; set { _apiUrl = value; OnPropertyChanged(); } }

    private AuthMode _auth = AuthMode.ApiKey;
    public AuthMode Auth
    {
        get => _auth;
        set
        {
            _auth = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApiKeyVisible));
            OnPropertyChanged(nameof(UserPassVisible));
        }
    }

    private string _apiKey = "";
    public string ApiKey { get => _apiKey; set { _apiKey = value; OnPropertyChanged(); } }

    private string _username = "";
    public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }

    private string _password = "";
    public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

    [JsonIgnore]
    public bool AuthInitialized { get; set; }

    [JsonIgnore]
    public bool ApiKeyVisible => Auth == AuthMode.ApiKey;

    [JsonIgnore]
    public bool UserPassVisible => Auth == AuthMode.UserPass;

    public Source(int id)
    {
        Id = id;
    }
}
