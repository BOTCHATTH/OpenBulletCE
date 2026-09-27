using Avalonia.Controls;
using Avalonia.Layout;
using RuriLib;
using RuriLib.Functions.Requests;
using System;

namespace OpenBulletCE.Views.StackerBlocks;

public class PageBlockRequest : BlockPage
{
    private readonly BlockRequest _b;

    // Request-type sub-panels
    private readonly StackPanel _basicAuthPanel = new() { Spacing = 2 };
    private readonly StackPanel _standardPanel = new() { Spacing = 2 };
    private readonly StackPanel _multipartPanel = new() { Spacing = 2 };
    private readonly StackPanel _rawPanel = new() { Spacing = 2 };

    // Response-type sub-panels
    private readonly StackPanel _filePanel = new() { Spacing = 2 };
    private readonly StackPanel _base64Panel = new() { Spacing = 2 };

    // HTTP-library-dependent rows
    private StackPanel _browserPanel;
    private DockPanel _securityRow;

    /// <summary>curl-impersonate targets (mirrors libcurl-impersonate's supported list).</summary>
    private static readonly string[] CurlBrowserProfiles =
    {
        "chrome99","chrome100","chrome101","chrome104","chrome107","chrome110","chrome116",
        "chrome119","chrome120","chrome123","chrome124","chrome131","chrome133a","chrome136","chrome146",
        "chrome99_android","chrome131_android",
        "edge99","edge101",
        "safari15_3","safari15_5","safari17_0","safari17_2_ios","safari18_0","safari18_0_ios","safari26_0",
        "firefox133","firefox135","firefox144",
        "tor145"
    };

    public PageBlockRequest(BlockRequest block) : base(block)
    {
        _b = block;

        Panel.Children.Add(Row("URL:", Txt(nameof(BlockRequest.Url))));

        // Method + check grid
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("30,30"),
            Margin = new Avalonia.Thickness(0, 5, 0, 0)
        };
        var methodRow = new DockPanel();
        methodRow.Children.Add(new TextBlock { Text = "Method:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 6, 0) });
        methodRow.Children.Add(EnumCombo<Extreme.Net.HttpMethod>(_b.Method, v => _b.Method = v));
        Grid.SetColumnSpan(methodRow, 2);
        grid.Children.Add(methodRow);
        var autoRedir = Check("Auto Redirect", nameof(BlockRequest.AutoRedirect));
        Grid.SetColumn(autoRedir, 2);
        grid.Children.Add(autoRedir);
        var readSrc = Check("Read Resp. Source", nameof(BlockRequest.ReadResponseSource));
        Grid.SetRow(readSrc, 1);
        grid.Children.Add(readSrc);
        var acceptEnc = Check("Accept-Encoding", nameof(BlockRequest.AcceptEncoding));
        Grid.SetRow(acceptEnc, 1); Grid.SetColumn(acceptEnc, 1);
        grid.Children.Add(acceptEnc);
        var encContent = Check("Encode Content", nameof(BlockRequest.EncodeContent));
        Grid.SetRow(encContent, 1); Grid.SetColumn(encContent, 2);
        grid.Children.Add(encContent);
        Panel.Children.Add(grid);

        // HTTP Library (OB2 parity): SystemNet / RuriLibHttp / CurlImpersonate
        _browserPanel = new StackPanel { Spacing = 2 };
        _securityRow = Row("Security Protocol:", EnumCombo<SecurityProtocol>(_b.SecurityProtocol, v => _b.SecurityProtocol = v));

        Panel.Children.Add(new DockPanel
        {
            Margin = new Avalonia.Thickness(0, 5, 0, 0),
            Children =
            {
                new TextBlock { Text = "HTTP Library:", Foreground = Brush("BrushCustom"), VerticalAlignment = VerticalAlignment.Center, MinWidth = 110 },
                EnumCombo<HttpLibrary>(_b.HttpLibrary, v => { _b.HttpLibrary = v; UpdateLibraryPanels(); })
            }
        });

        // Curl impersonate options — browser profile + browser headers toggle
        var browserCombo = new ComboBox { ItemsSource = CurlBrowserProfiles, SelectedItem = _b.CurlBrowserProfile, MinWidth = 160 };
        browserCombo.SelectionChanged += (_, _) => { if (browserCombo.SelectedItem is string s) _b.CurlBrowserProfile = s; };
        _browserPanel.Children.Add(Row("Browser:", browserCombo));
        _browserPanel.Children.Add(Check("Use browser headers (ignores custom headers)", nameof(BlockRequest.CurlUseBrowserHeaders)));

        Panel.Children.Add(_securityRow);
        Panel.Children.Add(_browserPanel);
        UpdateLibraryPanels();

        // Request Type
        Panel.Children.Add(new DockPanel
        {
            Margin = new Avalonia.Thickness(0, 5, 0, 0),
            Children =
            {
                new TextBlock { Text = "Request Type:", Foreground = Brush("BrushCustom"), VerticalAlignment = VerticalAlignment.Center, MinWidth = 110 },
                EnumCombo<RequestType>(_b.RequestType, v => { _b.RequestType = v; UpdateRequestPanels(); })
            }
        });

        _basicAuthPanel.Children.Add(Row("Username:", Txt(nameof(BlockRequest.AuthUser))));
        _basicAuthPanel.Children.Add(Row("Password:", Txt(nameof(BlockRequest.AuthPass))));

        _standardPanel.Children.Add(Row("POST Data:", Txt(nameof(BlockRequest.PostData))));
        _standardPanel.Children.Add(Row("Content-Type:", Txt(nameof(BlockRequest.ContentType), "e.g. application/x-www-form-urlencoded, application/json")));

        _multipartPanel.Children.Add(Row("Boundary (random if empty):", Txt(nameof(BlockRequest.MultipartBoundary)), 200));
        _multipartPanel.Children.Add(new TextBlock { Text = "Multipart Contents:", Margin = new Avalonia.Thickness(0, 4, 0, 0) });
        _multipartPanel.Children.Add(MultiBox(_b.GetMultipartContents(), s => _b.SetMultipartContents(s.Split('\n', StringSplitOptions.RemoveEmptyEntries)), 60,
            "Syntax:\nSTRING:fieldname:fieldvalue\nFILE:fieldname:filepath:content-type"));

        _rawPanel.Children.Add(Row("Raw HEX Data:", Txt(nameof(BlockRequest.RawData))));
        _rawPanel.Children.Add(Row("Content-Type:", Txt(nameof(BlockRequest.ContentType))));

        Panel.Children.Add(_basicAuthPanel);
        Panel.Children.Add(_standardPanel);
        Panel.Children.Add(_multipartPanel);
        Panel.Children.Add(_rawPanel);

        Panel.Children.Add(new TextBlock { Text = "Custom Cookies:", Margin = new Avalonia.Thickness(0, 8, 0, 0) });
        Panel.Children.Add(MultiBox(_b.GetCustomCookies(), s => _b.SetCustomCookies(s.Split('\n', StringSplitOptions.RemoveEmptyEntries)), 80, "Syntax:\nname: value"));

        Panel.Children.Add(new TextBlock { Text = "Custom Headers:", Margin = new Avalonia.Thickness(0, 8, 0, 0) });
        Panel.Children.Add(MultiBox(_b.GetCustomHeaders(), s => _b.SetCustomHeaders(s.Split('\n', StringSplitOptions.RemoveEmptyEntries)), 80, "Syntax:\nname: value"));

        Panel.Children.Add(new DockPanel
        {
            Margin = new Avalonia.Thickness(0, 5, 0, 0),
            Children =
            {
                new TextBlock { Text = "Response Type:", Foreground = Brush("BrushCustom"), VerticalAlignment = VerticalAlignment.Center, MinWidth = 110 },
                EnumCombo<ResponseType>(_b.ResponseType, v => { _b.ResponseType = v; UpdateResponsePanels(); })
            }
        });

        _filePanel.Children.Add(Row("File Path:", Txt(nameof(BlockRequest.DownloadPath))));
        _filePanel.Children.Add(Check("Save as screenshot", nameof(BlockRequest.SaveAsScreenshot)));

        _base64Panel.Children.Add(Row("Output Variable:", Txt(nameof(BlockRequest.OutputVariable))));

        Panel.Children.Add(_filePanel);
        Panel.Children.Add(_base64Panel);

        UpdateRequestPanels();
        UpdateResponsePanels();
    }

    private void UpdateRequestPanels()
    {
        _basicAuthPanel.IsVisible = _b.RequestType == RequestType.BasicAuth;
        _standardPanel.IsVisible = _b.RequestType == RequestType.Standard;
        _multipartPanel.IsVisible = _b.RequestType == RequestType.Multipart;
        _rawPanel.IsVisible = _b.RequestType == RequestType.Raw;
    }

    private void UpdateResponsePanels()
    {
        _filePanel.IsVisible = _b.ResponseType == ResponseType.File;
        _base64Panel.IsVisible = _b.ResponseType == ResponseType.Base64String;
    }

    private void UpdateLibraryPanels()
    {
        var curl = _b.HttpLibrary == HttpLibrary.CurlImpersonate;
        _securityRow.IsVisible = !curl;
        _browserPanel.IsVisible = curl;
    }
}
