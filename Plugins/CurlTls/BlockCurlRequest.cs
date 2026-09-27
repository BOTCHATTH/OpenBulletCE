using Extreme.Net;
using PluginFramework;
using RuriLib;
using RuriLib.LS;
using RuriLib.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace CurlTls;

/// <summary>
/// LoliScript block plugin — performs an HTTP request through libcurl-impersonate
/// so the TLS/JA3 + HTTP/2 fingerprints match a real browser.
///
/// Usage:
///   CURLREQUEST "https://example.com" BROWSER "chrome120" METHOD "POST" CONTENT "a=b" CTYPE "application/json" -> VAR "RESP"
/// </summary>
public class BlockCurlRequest : BlockBase, IBlockPlugin
{
    public string Name => "CURLREQUEST";
    public string Color => "#1abc9c";
    public bool LightForeground => true;

    private string url = "https://";
    /// <summary>The URL to request.</summary>
    public string Url { get => url; set { url = value; OnPropertyChanged(); } }

    private string browserProfile = "chrome120";
    /// <summary>curl-impersonate target (chrome99..chrome146, safari*, firefox*, edge*, tor*).</summary>
    public string BrowserProfile { get => browserProfile; set { browserProfile = value; OnPropertyChanged(); } }

    private string method = "GET";
    /// <summary>The HTTP method.</summary>
    public string Method { get => method; set { method = value.ToUpper(); OnPropertyChanged(); } }

    private string postData = "";
    /// <summary>The request body (for POST/PUT/PATCH).</summary>
    public string PostData { get => postData; set { postData = value; OnPropertyChanged(); } }

    private string contentType = "application/x-www-form-urlencoded";
    /// <summary>The Content-Type header sent with a body.</summary>
    public string ContentType { get => contentType; set { contentType = value; OnPropertyChanged(); } }

    private bool autoRedirect = true;
    /// <summary>Follow redirects.</summary>
    public bool AutoRedirect { get => autoRedirect; set { autoRedirect = value; OnPropertyChanged(); } }

    private int timeoutSeconds = 30;
    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get => timeoutSeconds; set { timeoutSeconds = value; OnPropertyChanged(); } }

    private string variableName = "";
    /// <summary>Output variable when using -> VAR.</summary>
    public string VariableName { get => variableName; set { variableName = value; OnPropertyChanged(); } }

    private bool isCapture;
    /// <summary>Whether the output variable is a capture.</summary>
    public bool IsCapture { get => isCapture; set { isCapture = value; OnPropertyChanged(); } }

    /// <summary>Custom headers, one "name: value" per line.</summary>
    public List<string> CustomHeadersList { get; set; } = new();

    public BlockCurlRequest()
    {
        Label = "CURL";
    }

    public override BlockBase FromLS(string line)
    {
        var input = line.Trim();

        if (input.StartsWith("#"))
            Label = LineParser.ParseLabel(ref input);

        Url = LineParser.ParseLiteral(ref input, "URL");

        while (LineParser.Lookahead(ref input) == TokenType.Boolean)
            LineParser.SetBool(ref input, this);

        while (!string.IsNullOrEmpty(input) && !input.StartsWith("->"))
        {
            var parsed = LineParser.ParseToken(ref input, TokenType.Parameter, true).ToUpper();
            switch (parsed)
            {
                case "BROWSER":
                    BrowserProfile = LineParser.ParseLiteral(ref input, "BROWSER PROFILE");
                    break;
                case "METHOD":
                    Method = LineParser.ParseLiteral(ref input, "METHOD");
                    break;
                case "CONTENT":
                    PostData = LineParser.ParseLiteral(ref input, "POST DATA");
                    break;
                case "CTYPE":
                    ContentType = LineParser.ParseLiteral(ref input, "CONTENT TYPE");
                    break;
                case "HEADER":
                    CustomHeadersList.Add(LineParser.ParseLiteral(ref input, "HEADER"));
                    break;
            }
        }

        if (input.StartsWith("->"))
        {
            LineParser.EnsureIdentifier(ref input, "->");
            var outType = LineParser.ParseToken(ref input, TokenType.Parameter, true).ToUpper();
            if (outType == "CAP")
            {
                IsCapture = true;
                VariableName = LineParser.ParseLiteral(ref input, "OUTPUT VARIABLE");
            }
            else if (outType == "VAR")
            {
                VariableName = LineParser.ParseLiteral(ref input, "OUTPUT VARIABLE");
            }
        }

        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var writer = new BlockWriter(GetType(), indent, Disabled);
        writer
            .Label(Label)
            .Token("CURLREQUEST")
            .Literal(Url)
            .Literal(BrowserProfile, "BrowserProfile")
            .Indent()
            .Token("METHOD").Literal(Method, "Method")
            .Token("CONTENT").Literal(PostData, "PostData")
            .Token("CTYPE").Literal(ContentType, "ContentType")
            .Boolean(AutoRedirect, "AutoRedirect");

        foreach (var h in CustomHeadersList)
            writer.Token("HEADER").Literal(h);

        if (!string.IsNullOrEmpty(VariableName))
            writer.Indent().Arrow()
                .Token(IsCapture ? "CAP" : "VAR")
                .Literal(VariableName);

        return writer.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        var body = new MemoryStream();
        var headers = new MemoryStream();

        Curl.WriteCallback writeCb = (ptr, size, nmemb, _) =>
        {
            var n = (int)(size * nmemb);
            if (n > 0)
            {
                var buf = new byte[n];
                Marshal.Copy(ptr, buf, 0, n);
                body.Write(buf, 0, n);
            }
            return size * nmemb;
        };
        Curl.WriteCallback headerCb = (ptr, size, nmemb, _) =>
        {
            var n = (int)(size * nmemb);
            if (n > 0)
            {
                var buf = new byte[n];
                Marshal.Copy(ptr, buf, 0, n);
                headers.Write(buf, 0, n);
            }
            return size * nmemb;
        };

        var h = Curl.curl_easy_init();
        if (h == IntPtr.Zero)
        {
            data.Log(new LogEntry("curl_easy_init failed", Colors.Tomato));
            return;
        }

        var slist = IntPtr.Zero;
        try
        {
            // TLS/HTTP fingerprint impersonation
            var rc = Curl.curl_easy_impersonate(h, BrowserProfile, 1);
            if (rc != 0)
                data.Log(new LogEntry($"curl_easy_impersonate '{BrowserProfile}' returned {rc} — continuing anyway", Colors.DarkOrange));

            var localUrl = ReplaceValues(Url, data);
            var localData = ReplaceValues(PostData, data);

            Curl.SetoptStr(h, Curl.OPT_URL, localUrl);
            Curl.SetoptCb(h, Curl.OPT_WRITEFUNCTION, writeCb);
            Curl.SetoptCb(h, Curl.OPT_HEADERFUNCTION, headerCb);
            Curl.SetoptLong(h, Curl.OPT_TIMEOUT_MS, TimeoutSeconds * 1000L);
            Curl.SetoptLong(h, Curl.OPT_FOLLOWLOCATION, AutoRedirect ? 1 : 0);
            Curl.SetoptLong(h, Curl.OPT_SSL_VERIFYPEER, 0);
            Curl.SetoptLong(h, Curl.OPT_SSL_VERIFYHOST, 0);
            Curl.SetoptStr(h, Curl.OPT_ENCODING, "");

            var method = Method.ToUpperInvariant();
            Curl.SetoptStr(h, Curl.OPT_CUSTOMREQUEST, method);

            if (method != "GET" && method != "HEAD" && !string.IsNullOrEmpty(localData))
            {
                var bytes = Encoding.UTF8.GetBytes(localData);
                var buf = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, buf, bytes.Length);
                Curl.SetoptPtr(h, Curl.OPT_POSTFIELDS, buf);
                Curl.SetoptLong(h, Curl.OPT_POSTFIELDSIZE_LARGE, bytes.Length);
                slist = Curl.curl_slist_append(slist, $"Content-Type: {ContentType}");
            }

            // Custom headers
            foreach (var hdr in CustomHeadersList)
                slist = Curl.curl_slist_append(slist, ReplaceValues(hdr, data));

            if (slist != IntPtr.Zero)
                Curl.SetoptPtr(h, Curl.OPT_HTTPHEADER, slist);

            // Cookies in
            if (data.Cookies?.Count > 0)
                Curl.SetoptStr(h, Curl.OPT_COOKIE,
                    string.Join("; ", data.Cookies.Select(c => $"{c.Key}={c.Value}")));

            // Proxy
            if (data.UseProxies && data.Proxy != null)
            {
                Curl.SetoptStr(h, Curl.OPT_PROXY, $"{data.Proxy.Host}:{data.Proxy.Port}");
                Curl.SetoptLong(h, Curl.OPT_PROXYTYPE, data.Proxy.Type switch
                {
                    ProxyType.Socks4 => Curl.PROXY_SOCKS4,
                    ProxyType.Socks4a => Curl.PROXY_SOCKS4A,
                    ProxyType.Socks5 => Curl.PROXY_SOCKS5,
                    _ => Curl.PROXY_HTTP
                });
                if (!string.IsNullOrEmpty(data.Proxy.Username))
                    Curl.SetoptStr(h, Curl.OPT_PROXYUSERPWD, $"{data.Proxy.Username}:{data.Proxy.Password}");
            }

            data.Log(new LogEntry($"Calling Address: {localUrl}", Colors.Gainsboro));
            data.Log(new LogEntry($"Browser Profile: {BrowserProfile}", Colors.Gainsboro));

            rc = Curl.curl_easy_perform(h);
            if (rc != 0)
            {
                data.ResponseCode = "0";
                data.ResponseSource = $"curl error {rc}: {Marshal.PtrToStringAnsi(Curl.curl_easy_strerror(rc))}";
                data.Log(new LogEntry(data.ResponseSource, Colors.Tomato));
                return;
            }

            Curl.GetInfoLong(h, Curl.INFO_RESPONSE_CODE, out var code);
            data.ResponseCode = code.ToString();

            // Parse response headers + Set-Cookie merge
            var rawHeaders = Encoding.UTF8.GetString(headers.ToArray());
            data.ResponseHeaders.Clear();
            foreach (var hl in rawHeaders.Split('\n'))
            {
                var line2 = hl.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line2)) continue;
                var idx = line2.IndexOf(':');
                if (idx <= 0) continue;
                var k = line2[..idx].Trim();
                var v = line2[(idx + 1)..].Trim();
                if (k.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    var kv = v.Split(';')[0];
                    var eq = kv.IndexOf('=');
                    if (eq > 0) data.Cookies[kv[..eq].Trim()] = kv[(eq + 1)..].Trim();
                }
                else
                    data.ResponseHeaders[k] = v;
            }

            data.ResponseSource = Encoding.UTF8.GetString(body.ToArray());
            data.Log(new LogEntry($"Response code: {data.ResponseCode} ({body.Length} bytes)", Colors.Gainsboro));

            if (!string.IsNullOrEmpty(VariableName))
                InsertVariable(data, IsCapture, data.ResponseSource, VariableName);
        }
        finally
        {
            if (slist != IntPtr.Zero) Curl.curl_slist_free_all(slist);
            Curl.curl_easy_cleanup(h);
        }
    }
}
