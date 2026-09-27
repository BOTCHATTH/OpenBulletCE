using Extreme.Net;
using RuriLib.Functions.Formats;
using RuriLib.LS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RuriLib.Models;
using RuriLib.Functions.Requests;
using RuriLib.Functions.Files;
using MultipartContent = RuriLib.Functions.Requests.MultipartContent;

namespace RuriLib
{
    /// <summary>
    /// The types of request that can be performed.
    /// </summary>
    public enum RequestType
    {
        /// <summary>A standard request with standard content.</summary>
        Standard,

        /// <summary>A request which uses the 'Authentication: Basic' header.</summary>
        BasicAuth,

        /// <summary>A request which contains multipart content (strings and/or files).</summary>
        Multipart,

        /// <summary>A request which sends a raw byte stream.</summary>
        Raw
    }

    /// <summary>
    /// The available types of multipart contents.
    /// </summary>
    public enum MultipartContentType
    {
        /// <summary>A string content.</summary>
        String,

        /// <summary>A file content.</summary>
        File
    }

    /// <summary>
    /// The type of data expected inside the HTTP response.
    /// </summary>
    public enum ResponseType
    {
        /// <summary>A string response, e.g. an HTML page.</summary>
        String,

        /// <summary>A file response, e.g. an image.</summary>
        File,

        /// <summary>A byte array response encoded as a base64 string.</summary>
        Base64String
    }

    /// <summary>
    /// A block that can perform HTTP requests.
    /// </summary>
    public class BlockRequest : BlockBase
    {
        #region Variables
        private string url = "https://google.com";
        /// <summary>The URL to call, including additional GET query parameters.</summary>
        public string Url { get { return url; } set { url = value; OnPropertyChanged(); } }

        private RequestType requestType = RequestType.Standard;
        /// <summary>The request type.</summary>
        public RequestType RequestType { get { return requestType; } set { requestType = value; OnPropertyChanged(); } }

        // Basic Auth
        private string authUser = "";
        /// <summary>The username for basic auth requests.</summary>
        public string AuthUser { get { return authUser; } set { authUser = value; OnPropertyChanged(); } }

        private string authPass = "";
        /// <summary>The password for basic auth requests.</summary>
        public string AuthPass { get { return authPass; } set { authPass = value; OnPropertyChanged(); } }

        // Standard
        private string postData = "";
        /// <summary>The content of the request, sent after the headers. Use '\n' to input a linebreak.</summary>
        public string PostData { get { return postData; } set { postData = value; OnPropertyChanged(); } }

        // Raw
        private string rawData = "";
        /// <summary>The content of the request as a raw HEX string that will be sent as a bytestream.</summary>
        public string RawData { get { return rawData; } set { rawData = value; OnPropertyChanged(); } }

        private Extreme.Net.HttpMethod method = Extreme.Net.HttpMethod.GET;
        /// <summary>The method of the HTTP request.</summary>
        public Extreme.Net.HttpMethod Method { get { return method; } set { method = value; OnPropertyChanged(); } }

        private SecurityProtocol securityProtocol = SecurityProtocol.SystemDefault;
        /// <summary>The security protocol(s) to use for the HTTPS request.</summary>
        public SecurityProtocol SecurityProtocol { get { return securityProtocol; } set { securityProtocol = value; OnPropertyChanged(); } }

        private HttpLibrary httpLibrary = HttpLibrary.RuriLibHttp;
        /// <summary>Which HTTP engine performs the request (SystemNet / RuriLibHttp / CurlImpersonate).</summary>
        public HttpLibrary HttpLibrary { get { return httpLibrary; } set { httpLibrary = value; OnPropertyChanged(); } }

        private string curlBrowserProfile = "chrome120";
        /// <summary>curl-impersonate target (chrome99..chrome146, safari*, firefox*, edge*, tor*).</summary>
        public string CurlBrowserProfile { get { return curlBrowserProfile; } set { curlBrowserProfile = value; OnPropertyChanged(); } }

        private bool curlUseBrowserHeaders = true;
        /// <summary>If true, curl-impersonate sends browser-default headers and custom headers are ignored.</summary>
        public bool CurlUseBrowserHeaders { get { return curlUseBrowserHeaders; } set { curlUseBrowserHeaders = value; OnPropertyChanged(); } }

        /// <summary>The custom headers that are sent in the HTTP request.</summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>() {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36" },
            { "Pragma", "no-cache" },
            { "Accept", "*/*" }
        };

        /// <summary>The custom cookies that are sent in the HTTP request.</summary>
        private List<string> CustomCookiesList { get; set; } = new List<string>() { };

        private string contentType = "application/x-www-form-urlencoded";
        /// <summary>The type of content the server should expect.</summary>
        public string ContentType { get { return contentType; } set { contentType = value; OnPropertyChanged(); } }

        private bool autoRedirect = true;
        /// <summary>Whether to perform automatic redirection in the case of 3xx headers.</summary>
        public bool AutoRedirect { get { return autoRedirect; } set { autoRedirect = value; OnPropertyChanged(); } }

        private bool readResponseSource = true;
        /// <summary>Whether to read the stream of data from the HTTP response. Set to false if only the headers are needed, in order to speed up the process.</summary>
        public bool ReadResponseSource { get { return readResponseSource; } set { readResponseSource = value; OnPropertyChanged(); } }

        private bool encodeContent = false;
        /// <summary>Whether to URL encode the content before sending it.</summary>
        public bool EncodeContent { get { return encodeContent; } set { encodeContent = value; OnPropertyChanged(); } }

        private bool acceptEncoding = true;
        /// <summary>Whether to automatically generate an Accept-Encoding header.</summary>
        public bool AcceptEncoding { get { return acceptEncoding; } set { acceptEncoding = value; OnPropertyChanged(); } }

        // Multipart
        private string multipartBoundary = "";
        /// <summary>The boundary that separates multipart contents.</summary>
        public string MultipartBoundary { get { return multipartBoundary; } set { multipartBoundary = value; OnPropertyChanged(); } }

        /// <summary>The list of contents to send in a multipart request.</summary>
        public List<MultipartContent> MultipartContents { get; set; } = new List<MultipartContent>();

        private ResponseType responseType = ResponseType.String;
        /// <summary>The type of response expected from the server.</summary>
        public ResponseType ResponseType { get { return responseType; } set { responseType = value; OnPropertyChanged(); } }

        private string downloadPath = "";
        /// <summary>The path of the file where a FILE response needs to be stored.</summary>
        public string DownloadPath { get { return downloadPath; } set { downloadPath = value; OnPropertyChanged(); } }

        private string outputVariable = "";
        /// <summary>The variable name for Base64String response.</summary>
        public string OutputVariable { get { return outputVariable; } set { outputVariable = value; OnPropertyChanged(); } }
        
        private bool saveAsScreenshot = false;
        /// <summary>Whether to add the downloaded image to the default screenshot path.</summary>
        public bool SaveAsScreenshot { get { return saveAsScreenshot; } set { saveAsScreenshot = value; OnPropertyChanged(); } }
        #endregion

        /// <summary>
        /// Creates a Request block.
        /// </summary>
        public BlockRequest()
        {
            Label = "REQUEST";
        }

        /// <inheritdoc />
        public override BlockBase FromLS(string line)
        {
            // Trim the line
            var input = line.Trim();

            // Parse the label
            if (input.StartsWith("#"))
                Label = LineParser.ParseLabel(ref input);

            Method = (Extreme.Net.HttpMethod)LineParser.ParseEnum(ref input, "METHOD", typeof (Extreme.Net.HttpMethod));
            Url = LineParser.ParseLiteral(ref input, "URL");

            while (LineParser.Lookahead(ref input) == TokenType.Boolean)
                LineParser.SetBool(ref input, this);

            CustomHeaders.Clear(); // Remove the default headers

            while (input != string.Empty && !input.StartsWith("->"))
            {
                var parsed = LineParser.ParseToken(ref input, TokenType.Parameter, true).ToUpper();
                switch (parsed)
                {
                    case "MULTIPART":
                        RequestType = RequestType.Multipart;
                        break;

                    case "BASICAUTH":
                        RequestType = RequestType.BasicAuth;
                        break;

                    case "STANDARD":
                        RequestType = RequestType.Standard;
                        break;

                    case "RAW":
                        RequestType = RequestType.Raw;
                        break;

                    case "CONTENT":
                        PostData = LineParser.ParseLiteral(ref input, "POST DATA");
                        break;

                    case "RAWDATA":
                        RawData = LineParser.ParseLiteral(ref input, "RAW DATA");
                        break;

                    case "STRINGCONTENT":
                        var stringContentPair = ParseString(LineParser.ParseLiteral(ref input, "STRING CONTENT"), ':', 2);
                        MultipartContents.Add(new MultipartContent() { Type = MultipartContentType.String, Name = stringContentPair[0], Value = stringContentPair[1] });
                        break;

                    case "FILECONTENT":
                        var fileContentTriplet = ParseString(LineParser.ParseLiteral(ref input, "FILE CONTENT"), ':', 3);
                        MultipartContents.Add(new MultipartContent() { Type = MultipartContentType.File, Name = fileContentTriplet[0], Value = fileContentTriplet[1], ContentType = fileContentTriplet[2] });
                        break;

                    case "COOKIE":
                        var cookiePair = LineParser.ParseLiteral(ref input, "COOKIE VALUE");
                        CustomCookiesList.Add(cookiePair);
                        break;

                    case "HEADER":
                        var headerPair = ParseString(LineParser.ParseLiteral(ref input, "HEADER VALUE"), ':', 2);
                        CustomHeaders[headerPair[0]] = headerPair[1];
                        break;

                    case "CONTENTTYPE":
                        ContentType = LineParser.ParseLiteral(ref input, "CONTENT TYPE");
                        break;

                    case "USERNAME":
                        AuthUser = LineParser.ParseLiteral(ref input, "USERNAME");
                        break;

                    case "PASSWORD":
                        AuthPass = LineParser.ParseLiteral(ref input, "PASSWORD");
                        break;

                    case "BOUNDARY":
                        MultipartBoundary = LineParser.ParseLiteral(ref input, "BOUNDARY");
                        break;

                    case "SECPROTO":
                        SecurityProtocol = LineParser.ParseEnum(ref input, "Security Protocol", typeof(SecurityProtocol));
                        break;

                    case "HTTPLIB":
                        HttpLibrary = LineParser.ParseEnum(ref input, "HTTP Library", typeof(HttpLibrary));
                        break;

                    case "BROWSER":
                        CurlBrowserProfile = LineParser.ParseLiteral(ref input, "BROWSER PROFILE");
                        break;

                    default:
                        break;
                }
            }

            if (input.StartsWith("->"))
            {
                LineParser.EnsureIdentifier(ref input, "->");
                var outType = LineParser.ParseToken(ref input, TokenType.Parameter, true);
                if (outType.ToUpper() == "STRING") ResponseType = ResponseType.String;
                else if (outType.ToUpper() == "FILE")
                {
                    ResponseType = ResponseType.File;
                    DownloadPath = LineParser.ParseLiteral(ref input, "DOWNLOAD PATH");
                    while (LineParser.Lookahead(ref input) == TokenType.Boolean)
                    {
                        LineParser.SetBool(ref input, this);
                    }
                }
                else if (outType.ToUpper() == "BASE64")
                {
                    ResponseType = ResponseType.Base64String;
                    OutputVariable = LineParser.ParseLiteral(ref input, "OUTPUT VARIABLE");
                }
            }

            return this;
        }

        /// <summary>
        /// Parses values from a string.
        /// </summary>
        /// <param name="input">The string to parse</param>
        /// <param name="separator">The character that separates the elements</param>
        /// <param name="count">The number of elements to return</param>
        /// <returns>The array of the parsed elements.</returns>
        public static string[] ParseString(string input, char separator, int count)
        {
            return input.Split(new[] { separator }, count).Select(s => s.Trim()).ToArray();
        }

        /// <inheritdoc />
        public override string ToLS(bool indent = true)
        {
            var writer = new BlockWriter(GetType(), indent, Disabled);
            writer
                .Label(Label)
                .Token("REQUEST")
                .Token(Method)
                .Literal(Url)
                .Boolean(AcceptEncoding, "AcceptEncoding")
                .Boolean(AutoRedirect, "AutoRedirect")
                .Boolean(ReadResponseSource, "ReadResponseSource")
                .Boolean(EncodeContent, "EncodeContent")
                .Token(RequestType, "RequestType")
                .Indent();

            switch (RequestType)
            {
                case RequestType.BasicAuth:
                    writer
                        .Token("USERNAME")
                        .Literal(AuthUser)
                        .Token("PASSWORD")
                        .Literal(AuthPass)
                        .Indent();
                    break;

                case RequestType.Standard:
                    if (HttpRequest.CanContainRequestBody(method))
                    {
                        writer
                            .Token("CONTENT")
                            .Literal(PostData)
                            .Indent()
                            .Token("CONTENTTYPE")
                            .Literal(ContentType);
                    }
                    break;

                case RequestType.Multipart:
                    foreach(var c in MultipartContents)
                    {
                        writer
                            .Indent()
                            .Token($"{c.Type.ToString().ToUpper()}CONTENT");

                        if (c.Type == MultipartContentType.String)
                        {
                            writer.Literal($"{c.Name}: {c.Value}");
                        }
                        else if (c.Type == MultipartContentType.File)
                        {
                            writer.Literal($"{c.Name}: {c.Value}: {c.ContentType}");
                        }
                    }
                    if (!writer.CheckDefault(MultipartBoundary, "MultipartBoundary"))
                    {
                        writer
                            .Indent()
                            .Token("BOUNDARY")
                            .Literal(MultipartBoundary);
                    }
                    break;

                case RequestType.Raw:
                    if (HttpRequest.CanContainRequestBody(method))
                    {
                        writer
                            .Token("RAWDATA")
                            .Literal(RawData)
                            .Indent()
                            .Token("CONTENTTYPE")
                            .Literal(ContentType);
                    }
                    break;
            }

            if (SecurityProtocol != SecurityProtocol.SystemDefault)
            {
                writer
                    .Indent()
                    .Token("SECPROTO")
                    .Token(SecurityProtocol, "SecurityProtocol");
            }

            if (HttpLibrary != HttpLibrary.RuriLibHttp)
            {
                writer
                    .Indent()
                    .Token("HTTPLIB")
                    .Token(HttpLibrary, "HttpLibrary");
            }

            if (HttpLibrary == HttpLibrary.CurlImpersonate)
            {
                writer
                    .Indent()
                    .Token("BROWSER")
                    .Literal(CurlBrowserProfile)
                    .Boolean(CurlUseBrowserHeaders, "CurlUseBrowserHeaders");
            }

            foreach (var c in CustomCookiesList)
            {
                writer
                    .Indent()
                    .Token("COOKIE")
                    .Literal(c);
            }

            foreach (var h in CustomHeaders)
            {
                writer
                    .Indent()
                    .Token("HEADER")
                    .Literal($"{h.Key}: {h.Value}");
            }

            if (ResponseType == ResponseType.File)
            {
                writer
                    .Indent()
                    .Arrow()
                    .Token("FILE")
                    .Literal(DownloadPath)
                    .Boolean(SaveAsScreenshot, "SaveAsScreenshot");
            }
            else if (ResponseType == ResponseType.Base64String)
            {
                writer
                    .Indent()
                    .Arrow()
                    .Token("BASE64")
                    .Literal(OutputVariable);
            }

            return writer.ToString();
        }

        /// <inheritdoc />
        public override void Process(BotData data)
        {
            base.Process(data);

            switch (HttpLibrary)
            {
                case HttpLibrary.CurlImpersonate:
                    ProcessCurl(data);
                    return;
                case HttpLibrary.SystemNet:
                    ProcessSystemNet(data);
                    return;
            }

            // Setup
            var request = new Request();
            request.Setup(data.GlobalSettings, securityProtocol, AutoRedirect, data.ConfigSettings.MaxRedirects, AcceptEncoding);

            var localUrl = ReplaceValues(Url, data);
            data.Log(new LogEntry($"Calling URL: {localUrl}", Colors.MediumTurquoise));
            
            // Set content
            switch (RequestType)
            {
                case RequestType.Standard:
                    request.SetStandardContent(ReplaceValues(PostData, data), ReplaceValues(ContentType, data), Method, EncodeContent, GetLogBuffer(data));
                    break;

                case RequestType.BasicAuth:
                    request.SetBasicAuth(ReplaceValues(AuthUser, data), ReplaceValues(AuthPass, data));
                    break;

                case RequestType.Multipart:
                    var contents = MultipartContents.Select(m =>
                        new MultipartContent()
                        {
                            Name = ReplaceValues(m.Name, data),
                            Value = ReplaceValues(m.Value, data),
                            ContentType = ReplaceValues(m.Value, data),
                            Type = m.Type
                        });
                    request.SetMultipartContent(contents, ReplaceValues(MultipartBoundary, data), GetLogBuffer(data));
                    break;

                case RequestType.Raw:
                    request.SetRawContent(ReplaceValues(RawData, data), ReplaceValues(ContentType, data), Method, GetLogBuffer(data));
                    break;
            }

            // Set proxy
            if (data.UseProxies && data.Proxy != null)
            {
                request.SetProxy(data.Proxy);
            }
            // Set cookies
            data.Log(new LogEntry("Sent Cookies:", Colors.MediumTurquoise));

            SetCustomCookies(CustomCookiesList, data);
            //foreach (var cookie in CustomCookies) // Add new user-defined custom cookies to the bot's cookie jar
            //    data.Cookies[ReplaceValues(cookie.Key, data)] = ReplaceValues(cookie.Value, data);

            request.SetCookies(data.Cookies, GetLogBuffer(data));

            // Set headers
            data.Log(new LogEntry("Sent Headers:", Colors.DarkTurquoise));
            var headers = CustomHeaders.Select( h =>
                    new KeyValuePair<string, string> (ReplaceValues(h.Key, data), ReplaceValues(h.Value, data))
                ).ToDictionary(h => h.Key, h => h.Value);
            request.SetHeaders(headers, AcceptEncoding, GetLogBuffer(data));

            

            // End the request part
            data.LogNewLine();

            // Perform the request
            try
            {
                (data.Address, data.ResponseCode, data.ResponseHeaders, data.Cookies) = request.Perform(localUrl, Method, GetLogBuffer(data));
            }
            catch (Exception ex)
            {
                if (data.ConfigSettings.IgnoreResponseErrors)
                {
                    data.Log(new LogEntry(ex.Message, Colors.Tomato));
                    data.ResponseSource = ex.Message;
                    return;
                }
                throw;
            }

            // Save the response content
            switch (ResponseType)
            {
                case ResponseType.String:
                    data.ResponseSource = request.SaveString(ReadResponseSource, data.ResponseHeaders, GetLogBuffer(data));
                    break;

                case ResponseType.File:
                    if (SaveAsScreenshot)
                    {
                        Files.SaveScreenshot(request.GetResponseStream(), data); // Read the stream
                        data.Log(new LogEntry("File saved as screenshot", Colors.Green));
                    }
                    else
                    {
                        request.SaveFile(ReplaceValues(DownloadPath, data), GetLogBuffer(data));
                    }
                    break;

                case ResponseType.Base64String:
                    var base64 = Convert.ToBase64String(request.GetResponseStream().ToArray());
                    InsertVariable(data, false, base64, OutputVariable);
                    break;

                default:
                    break;
            }
        }

        #region Custom Cookies, Headers and Multipart Contents
        /// <summary>
        /// Builds a string containing custom cookies.
        /// </summary>
        /// <returns>One cookie per line, with name and value separated by a colon</returns>
        public string GetCustomCookies()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pair in CustomCookiesList)
            {
                sb.Append(pair);
                if (!pair.Equals(CustomCookiesList.Last())) sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sets custom cookies from an array of lines.
        /// </summary>
        /// <param name="lines">The lines containing the colon-separated name and value of the cookies</param>
        public void SetCustomCookies(string[] lines)
        { 
            CustomCookiesList.Clear();
            CustomCookiesList.AddRange(lines);
            
        }

        private void SetCustomCookies(List<string> lines, BotData data)
        {

            if (lines.Count.Equals(1) && !lines[0].Contains(':')) 
            {
                string cooki = ReplaceValues(lines[0], data);
                string[] splitcookies = cooki.Split(new[] { '\n' });
                foreach(var splitcooki in splitcookies)
                {
                    if (splitcooki.Contains(':'))
                    {
                        var split = splitcooki.Split(new[] { ':' }, 2);
                        data.Cookies[split[0].Trim()] = split[1].Trim();
                    }
                }
            }
            else
            {
                foreach(var line in lines)
                {
                    if (line.Contains(':'))
                    {
                        var split = line.Split(new[] { ':' }, 2);
                        data.Cookies[ReplaceValues(split[0].Trim(),data)] = ReplaceValues(split[1].Trim(),data);
                    }
                }
            }
        }

        /// <summary>
        /// Builds a string containing custom headers.
        /// </summary>
        /// <returns>One header per line, with name and value separated by a colon</returns>
        public string GetCustomHeaders()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pair in CustomHeaders)
            {
                sb.Append($"{pair.Key}: {pair.Value}");
                if (!pair.Equals(CustomHeaders.Last())) sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sets custom headers from an array of lines.
        /// </summary>
        /// <param name="lines">The lines containing the colon-separated name and value of the headers</param>
        public void SetCustomHeaders(string[] lines)
        {
            CustomHeaders.Clear();
            foreach (var line in lines)
            {
                if (line.Contains(':'))
                {
                    var split = line.Split(new[] { ':' }, 2);
                    CustomHeaders[split[0].Trim()] = split[1].Trim();
                }
            }
        }

        /// <summary>
        /// Builds a string containing multipart content.
        /// </summary>
        /// <returns>One content per line, with type, name and value separated by a colon</returns>
        public string GetMultipartContents()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var c in MultipartContents)
            {
                sb.Append($"{c.Type.ToString().ToUpper()}: {c.Name}: {c.Value}");
                if (!c.Equals(MultipartContents.Last())) sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sets multipart contents from an array of lines.
        /// </summary>
        /// <param name="lines">The lines containing the colon-separated type, name and value of the multipart contents</param>
        public void SetMultipartContents(string[] lines)
        {
            MultipartContents.Clear();
            foreach(var line in lines)
            {
                try
                {
                    var split = line.Split(new[] { ':' }, 3);
                    MultipartContents.Add(new MultipartContent() {
                        Type = (MultipartContentType)Enum.Parse(typeof(MultipartContentType), split[0].Trim(), true),
                        Name = split[1].Trim(),
                        Value = split[2].Trim()
                    });
                }
                catch { }
            }
        }

        #endregion

        private List<LogEntry> GetLogBuffer(BotData data) => data.GlobalSettings.General.EnableBotLog || data.IsDebug ? data.LogBuffer : null;

        /// <summary>
        /// Builds the request body bytes for the active <see cref="RequestType"/>.
        /// </summary>
        private byte[] BuildBodyBytes(BotData data, out string contentType)
        {
            contentType = ReplaceValues(ContentType, data);
            switch (RequestType)
            {
                case RequestType.Standard:
                    var pData = Regex.Replace(ReplaceValues(PostData, data), @"(?<!\\)\\n", Environment.NewLine).Unescape();
                    return Encoding.UTF8.GetBytes(pData);

                case RequestType.Raw:
                    var hex = ReplaceValues(RawData, data).Replace(" ", "");
                    var bytes = new byte[hex.Length / 2];
                    for (var i = 0; i < bytes.Length; i++)
                        bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    return bytes;

                case RequestType.Multipart:
                    var bdry = string.IsNullOrEmpty(MultipartBoundary) ? "--------------------------" + Guid.NewGuid().ToString("N")[..8] : ReplaceValues(MultipartBoundary, data);
                    contentType = $"multipart/form-data; boundary={bdry}";
                    var sb = new StringBuilder();
                    foreach (var c in MultipartContents)
                    {
                        var name = ReplaceValues(c.Name, data);
                        var value = ReplaceValues(c.Value, data);
                        sb.Append("--").Append(bdry).Append("\r\n");
                        if (c.Type == MultipartContentType.File)
                        {
                            var ct = ReplaceValues(c.ContentType, data);
                            sb.Append($"Content-Disposition: form-data; name=\"{name}\"; filename=\"{Path.GetFileName(value)}\"\r\n");
                            if (!string.IsNullOrEmpty(ct)) sb.Append($"Content-Type: {ct}\r\n");
                            sb.Append("\r\n").Append(File.Exists(value) ? File.ReadAllText(value) : value).Append("\r\n");
                        }
                        else
                        {
                            sb.Append($"Content-Disposition: form-data; name=\"{name}\"\r\n\r\n{value.Unescape()}\r\n");
                        }
                    }
                    sb.Append("--").Append(bdry).Append("--\r\n");
                    return Encoding.UTF8.GetBytes(sb.ToString());

                default:
                    return null;
            }
        }

        /// <summary>
        /// Fills <see cref="BotData"/> response fields and handles ResponseType output.
        /// </summary>
        private void SaveResponse(BotData data, byte[] bodyBytes)
        {
            switch (ResponseType)
            {
                case ResponseType.String:
                    data.ResponseSource = ReadResponseSource ? Encoding.UTF8.GetString(bodyBytes) : string.Empty;
                    if (GetLogBuffer(data) != null)
                    {
                        data.Log(new LogEntry("Response Source:", Colors.Green));
                        data.Log(new LogEntry(ReadResponseSource ? data.ResponseSource : "[SKIPPED]", Colors.GreenYellow));
                    }
                    break;

                case ResponseType.File:
                    if (SaveAsScreenshot)
                    {
                        Files.SaveScreenshot(new MemoryStream(bodyBytes), data);
                        data.Log(new LogEntry("File saved as screenshot", Colors.Green));
                    }
                    else
                    {
                        File.WriteAllBytes(ReplaceValues(DownloadPath, data), bodyBytes);
                    }
                    break;

                case ResponseType.Base64String:
                    InsertVariable(data, false, Convert.ToBase64String(bodyBytes), OutputVariable);
                    break;
            }
        }

        /// <summary>
        /// Merges Set-Cookie headers into the bot cookie jar.
        /// </summary>
        private void MergeResponseCookies(BotData data, string rawHeaders)
        {
            foreach (var hl in rawHeaders.Split('\n'))
            {
                var line = hl.TrimEnd('\r');
                if (!line.StartsWith("Set-Cookie:", StringComparison.OrdinalIgnoreCase)) continue;
                var kv = line[(line.IndexOf(':') + 1)..].Split(';')[0];
                var eq = kv.IndexOf('=');
                if (eq > 0) data.Cookies[kv[..eq].Trim()] = kv[(eq + 1)..].Trim();
            }
        }

        /// <summary>
        /// Performs the request through libcurl-impersonate (browser TLS/JA3 + HTTP/2 fingerprint).
        /// </summary>
        private void ProcessCurl(BotData data)
        {
            Curl.EnsureResolver();

            var body = new MemoryStream();
            var headerBuf = new MemoryStream();

            Curl.WriteCallback writeCb = (ptr, size, nmemb, _) =>
            {
                var n = (int)(size * nmemb);
                if (n > 0)
                {
                    var buf = new byte[n];
                    System.Runtime.InteropServices.Marshal.Copy(ptr, buf, 0, n);
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
                    System.Runtime.InteropServices.Marshal.Copy(ptr, buf, 0, n);
                    headerBuf.Write(buf, 0, n);
                }
                return size * nmemb;
            };

            var h = Curl.curl_easy_init();
            if (h == IntPtr.Zero)
            {
                data.Log(new LogEntry("curl_easy_init failed — is libcurl-impersonate.dll present?", Colors.Tomato));
                data.ResponseCode = "0";
                return;
            }

            var slist = IntPtr.Zero;
            var bodyPtr = IntPtr.Zero;
            try
            {
                var rc = Curl.curl_easy_impersonate(h, CurlBrowserProfile, CurlUseBrowserHeaders ? 1 : 0);
                if (rc != 0)
                    data.Log(new LogEntry($"curl_easy_impersonate '{CurlBrowserProfile}' returned {rc} — continuing anyway", Colors.DarkOrange));

                var localUrl = ReplaceValues(Url, data);
                data.Log(new LogEntry($"Calling URL: {localUrl} [curl-impersonate {CurlBrowserProfile}]", Colors.MediumTurquoise));

                Curl.SetoptStr(h, Curl.OPT_URL, localUrl);
                Curl.SetoptCb(h, Curl.OPT_WRITEFUNCTION, writeCb);
                Curl.SetoptCb(h, Curl.OPT_HEADERFUNCTION, headerCb);
                Curl.SetoptLong(h, Curl.OPT_TIMEOUT_MS, data.GlobalSettings.General.RequestTimeout * 1000L);
                Curl.SetoptLong(h, Curl.OPT_FOLLOWLOCATION, AutoRedirect ? 1 : 0);
                Curl.SetoptLong(h, Curl.OPT_MAXREDIRS, data.ConfigSettings.MaxRedirects);
                Curl.SetoptLong(h, Curl.OPT_SSL_VERIFYPEER, 0);
                Curl.SetoptLong(h, Curl.OPT_SSL_VERIFYHOST, 0);
                if (AcceptEncoding) Curl.SetoptStr(h, Curl.OPT_ENCODING, "");

                var method = Method.ToString().ToUpperInvariant();
                Curl.SetoptStr(h, Curl.OPT_CUSTOMREQUEST, method);

                var bodyBytes = BuildBodyBytes(data, out var bodyContentType);
                if (method != "GET" && method != "HEAD" && bodyBytes != null && bodyBytes.Length > 0)
                {
                    bodyPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(bodyBytes.Length);
                    System.Runtime.InteropServices.Marshal.Copy(bodyBytes, 0, bodyPtr, bodyBytes.Length);
                    Curl.SetoptPtr(h, Curl.OPT_POSTFIELDS, bodyPtr);
                    Curl.SetoptLong(h, Curl.OPT_POSTFIELDSIZE_LARGE, bodyBytes.Length);
                    if (!string.IsNullOrEmpty(bodyContentType))
                        slist = Curl.curl_slist_append(slist, $"Content-Type: {bodyContentType}");
                    GetLogBuffer(data)?.Add(new LogEntry($"Post Data: {Encoding.UTF8.GetString(bodyBytes)}", Colors.MediumTurquoise));
                }

                if (RequestType == RequestType.BasicAuth)
                    slist = Curl.curl_slist_append(slist,
                        $"Authorization: Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ReplaceValues(AuthUser, data)}:{ReplaceValues(AuthPass, data)}"))}");

                if (!CurlUseBrowserHeaders)
                {
                    var headers = CustomHeaders.Select(h2 =>
                        $"{ReplaceValues(h2.Key, data)}: {ReplaceValues(h2.Value, data)}");
                    foreach (var hdr in headers)
                        slist = Curl.curl_slist_append(slist, hdr);
                }

                var cookieIn = new List<string>();
                if (data.Cookies?.Count > 0)
                    cookieIn.AddRange(data.Cookies.Select(c => $"{c.Key}={c.Value}"));
                foreach (var c in CustomCookiesList)
                {
                    var split = ReplaceValues(c, data).Split(new[] { ':' }, 2);
                    if (split.Length == 2) cookieIn.Add($"{split[0].Trim()}={split[1].Trim()}");
                }
                if (cookieIn.Count > 0)
                    Curl.SetoptStr(h, Curl.OPT_COOKIE, string.Join("; ", cookieIn));

                if (GetLogBuffer(data) != null)
                {
                    data.Log(new LogEntry("Sent Cookies:", Colors.MediumTurquoise));
                    foreach (var c in cookieIn)
                    {
                        var eq = c.IndexOf('=');
                        data.Log(new LogEntry(eq > 0 ? $"{c[..eq]}: {c[(eq + 1)..]}" : c, Colors.MediumTurquoise));
                    }

                    data.Log(new LogEntry("Sent Headers:", Colors.DarkTurquoise));
                    if (CurlUseBrowserHeaders)
                        data.Log(new LogEntry($"[browser headers — {CurlBrowserProfile}]", Colors.MediumTurquoise));
                    else
                        foreach (var hdr in CustomHeaders)
                            data.Log(new LogEntry($"{ReplaceValues(hdr.Key, data)}: {ReplaceValues(hdr.Value, data)}", Colors.MediumTurquoise));

                    data.LogNewLine();
                }

                if (slist != IntPtr.Zero)
                    Curl.SetoptPtr(h, Curl.OPT_HTTPHEADER, slist);

                if (data.UseProxies && data.Proxy != null)
                {
                    Curl.SetoptStr(h, Curl.OPT_PROXY, $"{data.Proxy.Host}:{data.Proxy.Port}");
                    Curl.SetoptLong(h, Curl.OPT_PROXYTYPE, data.Proxy.Type switch
                    {
                        Extreme.Net.ProxyType.Socks4 => Curl.PROXY_SOCKS4,
                        Extreme.Net.ProxyType.Socks4a => Curl.PROXY_SOCKS4A,
                        Extreme.Net.ProxyType.Socks5 => Curl.PROXY_SOCKS5,
                        _ => Curl.PROXY_HTTP
                    });
                    if (!string.IsNullOrEmpty(data.Proxy.Username))
                        Curl.SetoptStr(h, Curl.OPT_PROXYUSERPWD, $"{data.Proxy.Username}:{data.Proxy.Password}");
                }

                rc = Curl.curl_easy_perform(h);
                if (rc != 0)
                {
                    var err = $"curl error {rc}: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(Curl.curl_easy_strerror(rc))}";
                    if (data.ConfigSettings.IgnoreResponseErrors)
                    {
                        data.ResponseCode = "0";
                        data.ResponseSource = err;
                        data.Log(new LogEntry(err, Colors.Tomato));
                        return;
                    }
                    throw new Exception(err);
                }

                Curl.GetInfoLong(h, Curl.INFO_RESPONSE_CODE, out var code);
                data.ResponseCode = code.ToString();

                var rawHeaders = Encoding.UTF8.GetString(headerBuf.ToArray());
                data.ResponseHeaders.Clear();
                foreach (var hl in rawHeaders.Split('\n'))
                {
                    var line2 = hl.TrimEnd('\r');
                    if (string.IsNullOrWhiteSpace(line2)) continue;
                    var idx = line2.IndexOf(':');
                    if (idx <= 0) continue;
                    var k = line2[..idx].Trim();
                    var v = line2[(idx + 1)..].Trim();
                    if (!k.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                        data.ResponseHeaders[k] = v;
                }

                data.Address = localUrl;

                if (GetLogBuffer(data) != null)
                {
                    data.Log(new LogEntry("Address: " + data.Address, Colors.Cyan));
                    data.Log(new LogEntry($"Response code: {data.ResponseCode} ({body.Length} bytes)", Colors.Yellow));
                    data.Log(new LogEntry("Received headers:", Colors.DeepPink));
                    foreach (var kv in data.ResponseHeaders)
                        data.Log(new LogEntry($"{kv.Key}: {kv.Value}", Colors.LightPink));
                    data.Log(new LogEntry("Received cookies:", Colors.Goldenrod));
                    foreach (var hl in rawHeaders.Split('\n'))
                    {
                        var line2 = hl.TrimEnd('\r');
                        if (!line2.StartsWith("Set-Cookie:", StringComparison.OrdinalIgnoreCase)) continue;
                        var kv2 = line2[(line2.IndexOf(':') + 1)..].Split(';')[0].Trim();
                        var eq = kv2.IndexOf('=');
                        if (eq > 0) data.Log(new LogEntry($"{kv2[..eq]}: {kv2[(eq + 1)..]}", Colors.LightGoldenrodYellow));
                    }
                }

                MergeResponseCookies(data, rawHeaders);
                SaveResponse(data, body.ToArray());
            }
            finally
            {
                if (bodyPtr != IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(bodyPtr);
                if (slist != IntPtr.Zero) Curl.curl_slist_free_all(slist);
                Curl.curl_easy_cleanup(h);
            }
        }

        /// <summary>
        /// Performs the request through the built-in .NET HttpClient stack.
        /// </summary>
        private void ProcessSystemNet(BotData data)
        {
            var localUrl = ReplaceValues(Url, data);
            data.Log(new LogEntry($"Calling URL: {localUrl} [SystemNet]", Colors.MediumTurquoise));

            var handler = new System.Net.Http.HttpClientHandler
            {
                AllowAutoRedirect = AutoRedirect,
                MaxAutomaticRedirections = data.ConfigSettings.MaxRedirects,
                AutomaticDecompression = AcceptEncoding
                    ? System.Net.DecompressionMethods.All
                    : System.Net.DecompressionMethods.None,
                UseCookies = false
            };
            if (SecurityProtocol != SecurityProtocol.SystemDefault)
                handler.SslProtocols = SecurityProtocol.ToSslProtocols();

            if (data.UseProxies && data.Proxy != null)
            {
                var scheme = data.Proxy.Type switch
                {
                    Extreme.Net.ProxyType.Socks4 => "socks4",
                    Extreme.Net.ProxyType.Socks4a => "socks4a",
                    Extreme.Net.ProxyType.Socks5 => "socks5",
                    _ => "http"
                };
                handler.Proxy = new System.Net.WebProxy($"{scheme}://{data.Proxy.Host}:{data.Proxy.Port}");
                if (!string.IsNullOrEmpty(data.Proxy.Username))
                    handler.Proxy.Credentials = new System.Net.NetworkCredential(data.Proxy.Username, data.Proxy.Password);
                handler.UseProxy = true;
            }

            using var client = new System.Net.Http.HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(data.GlobalSettings.General.RequestTimeout * 1000)
            };

            var method = new System.Net.Http.HttpMethod(Method.ToString().ToUpperInvariant());
            var req = new System.Net.Http.HttpRequestMessage(method, localUrl);

            var bodyBytes = BuildBodyBytes(data, out var bodyContentType);
            if (bodyBytes != null && bodyBytes.Length > 0 && method != System.Net.Http.HttpMethod.Get && method != System.Net.Http.HttpMethod.Head)
            {
                req.Content = new System.Net.Http.ByteArrayContent(bodyBytes);
                if (!string.IsNullOrEmpty(bodyContentType))
                    req.Content.Headers.TryAddWithoutValidation("Content-Type", bodyContentType);
                GetLogBuffer(data)?.Add(new LogEntry($"Post Data: {Encoding.UTF8.GetString(bodyBytes)}", Colors.MediumTurquoise));
            }

            if (RequestType == RequestType.BasicAuth)
                req.Headers.TryAddWithoutValidation("Authorization",
                    $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ReplaceValues(AuthUser, data)}:{ReplaceValues(AuthPass, data)}"))}");

            var cookieIn = new List<string>();
            if (data.Cookies?.Count > 0)
                cookieIn.AddRange(data.Cookies.Select(c => $"{c.Key}={c.Value}"));
            foreach (var c in CustomCookiesList)
            {
                var split = ReplaceValues(c, data).Split(new[] { ':' }, 2);
                if (split.Length == 2) cookieIn.Add($"{split[0].Trim()}={split[1].Trim()}");
            }
            if (cookieIn.Count > 0)
                req.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookieIn));

            if (GetLogBuffer(data) != null)
            {
                data.Log(new LogEntry("Sent Cookies:", Colors.MediumTurquoise));
                foreach (var c in cookieIn)
                {
                    var eq = c.IndexOf('=');
                    data.Log(new LogEntry(eq > 0 ? $"{c[..eq]}: {c[(eq + 1)..]}" : c, Colors.MediumTurquoise));
                }
            }

            data.Log(new LogEntry("Sent Headers:", Colors.DarkTurquoise));
            foreach (var hdr in CustomHeaders)
            {
                var k = ReplaceValues(hdr.Key, data);
                var v = ReplaceValues(hdr.Value, data);
                GetLogBuffer(data)?.Add(new LogEntry($"{k}: {v}", Colors.MediumTurquoise));
                if (!req.Headers.TryAddWithoutValidation(k, v))
                    req.Content?.Headers.TryAddWithoutValidation(k, v);
            }
            if (GetLogBuffer(data) != null) data.LogNewLine();

            System.Net.Http.HttpResponseMessage resp;
            try
            {
                resp = client.SendAsync(req, ReadResponseSource
                    ? System.Net.Http.HttpCompletionOption.ResponseContentRead
                    : System.Net.Http.HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (data.ConfigSettings.IgnoreResponseErrors)
                {
                    data.Log(new LogEntry(ex.Message, Colors.Tomato));
                    data.ResponseSource = ex.Message;
                    data.ResponseCode = "0";
                    return;
                }
                throw;
            }

            using (resp)
            {
                data.ResponseCode = ((int)resp.StatusCode).ToString();
                data.Address = resp.RequestMessage?.RequestUri?.ToString() ?? localUrl;

                data.ResponseHeaders.Clear();
                var allHeaders = resp.Headers.Concat(resp.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>());
                var rawHeaderText = new StringBuilder();
                foreach (var hdr in allHeaders)
                {
                    var v = string.Join(", ", hdr.Value);
                    rawHeaderText.Append(hdr.Key).Append(": ").Append(v).Append('\n');
                    if (!hdr.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                        data.ResponseHeaders[hdr.Key] = v;
                }
                if (GetLogBuffer(data) != null)
                {
                    data.Log(new LogEntry("Address: " + data.Address, Colors.Cyan));
                    data.Log(new LogEntry($"Response code: {data.ResponseCode} ({resp.StatusCode})", Colors.Yellow));
                    data.Log(new LogEntry("Received headers:", Colors.DeepPink));
                    foreach (var kv in data.ResponseHeaders)
                        data.Log(new LogEntry($"{kv.Key}: {kv.Value}", Colors.LightPink));
                    data.Log(new LogEntry("Received cookies:", Colors.Goldenrod));
                    foreach (var hl in rawHeaderText.ToString().Split('\n'))
                    {
                        var line2 = hl.TrimEnd('\r');
                        if (!line2.StartsWith("Set-Cookie:", StringComparison.OrdinalIgnoreCase)) continue;
                        var kv2 = line2[(line2.IndexOf(':') + 1)..].Split(';')[0].Trim();
                        var eq = kv2.IndexOf('=');
                        if (eq > 0) data.Log(new LogEntry($"{kv2[..eq]}: {kv2[(eq + 1)..]}", Colors.LightGoldenrodYellow));
                    }
                }

                MergeResponseCookies(data, rawHeaderText.ToString());

                var body = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                SaveResponse(data, body);
            }
        }
    }
}
