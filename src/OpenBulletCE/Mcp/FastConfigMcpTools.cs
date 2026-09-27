using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace OpenBulletCE.Mcp;

/// <summary>
/// Fast config-making tools: write a whole script in one call, read script
/// files, validate, and get ready-made block templates.
/// </summary>
[McpServerToolType]
public sealed class FastConfigMcpTools
{
    /// <summary>Writes (or creates + writes) a config's full script in one call.</summary>
    [McpServerTool(Name = "write_config_script"),
     Description("REPLACES the entire LoliScript of a config (creates it if missing). For FULL rewrites only — to BUILD a config prefer add_block per block/construct (each call is parse-checked so you catch errors instantly). After writing, set settings via set_config_settings and verify via debug_config.")]
    public string WriteConfigScript(
        [Description("Config name")] string name,
        [Description("The complete LoliScript source")] string script,
        [Description("Category/subfolder (default 'Default')")] string category = "Default",
        [Description("Author (only used when creating)")] string author = "MCP")
    {
        if (string.IsNullOrWhiteSpace(script)) return "Script is empty.";

        var vm = McpConfigHelper.Find(name, category);
        if (vm == null)
        {
            // create it
            var res = McpConfigHelper.Create(name, category, author, script);
            if (res.StartsWith("Failed")) return res;
            vm = McpConfigHelper.Find(name, category);
            if (vm == null) return "Config creation failed.";
        }
        else
        {
            vm.Config.Script = script;
            McpConfigHelper.Save(vm);
        }

        return $"Script written ({ConfigScriptMcpTools.CountBlocks(script)} blocks). " +
               ConfigScriptMcpTools.ValidateLines(script);
    }

    /// <summary>Reads a LoliScript/text file from disk.</summary>
    [McpServerTool(Name = "read_script_file"),
     Description("Reads a text/LoliScript file from disk by path — e.g. an existing .lce or a snippet file the agent produced.")]
    public string ReadScriptFile(
        [Description("Absolute or relative file path")] string path)
    {
        try
        {
            if (!File.Exists(path)) return $"File not found: {path}";
            var text = File.ReadAllText(path);
            return text.Length > 200_000 ? text.Substring(0, 200_000) + "\n...(truncated)" : text;
        }
        catch (Exception ex) { return $"Read failed: {ex.Message}"; }
    }

    /// <summary>Validates a LoliScript snippet without saving it anywhere.</summary>
    [McpServerTool(Name = "validate_loliscript"),
     Description("Validates an arbitrary LoliScript source — every block line is parsed. Returns per-line errors. Always run this before write_config_script.")]
    public string ValidateLoliScript(
        [Description("The LoliScript source to validate")] string script)
        => ConfigScriptMcpTools.ValidateLines(script);

    /// <summary>Returns a ready-made template line for a block type.</summary>
    [McpServerTool(Name = "get_block_template"),
     Description("Returns a ready-to-paste LoliScript template line for a block type with placeholder args — REQUEST, PARSE, KEYCHECK, FUNCTION, UTILITY, COOKIECONTAINER, TCP, NAVIGATE, ELEMENTACTION, EXECUTEJS, BYPASSCF, CAPTCHA, RECAPTCHA, SOLVECAPTCHA.")]
    public string GetBlockTemplate(
        [Description("Block type keyword")] string blockType)
    {
        var key = blockType.Trim().ToUpperInvariant();
        if (Templates.TryGetValue(key, out var t)) return t;
        if (BlockReferenceMcpTools.BlockDocs.TryGetValue(key, out var doc)) return doc;
        return $"No template for '{blockType}'. See list_available_blocks / get_block_details.";
    }

    internal static readonly Dictionary<string, string> Templates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["REQUEST"] = """
// Basic GET
REQUEST GET "https://example.com/api"

// POST with form body
REQUEST POST "https://example.com/login" CONTENT "user=<USER>&pass=<PASS>" CONTENTTYPE "application/x-www-form-urlencoded"

// POST JSON + headers
REQUEST POST "https://api.example.com/v1" RAWDATA "{\"u\":\"<USER>\",\"p\":\"<PASS>\"}" CONTENTTYPE "application/json" HEADER "Authorization: Bearer <TOKEN>" HEADER "User-Agent: Mozilla/5.0"

// PRO STYLE — full browser header set (what real configs send; minimal requests get flagged):
REQUEST GET "https://api.example.com/me"
  COOKIE "st:<SESSION>"
  HEADER "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"
  HEADER "Accept: application/json, text/plain, */*"
  HEADER "Accept-Language: en-GB,en;q=0.5"
  HEADER "Accept-Encoding: gzip, deflate, br, zstd"
  HEADER "Origin: https://example.com"
  HEADER "Referer: https://example.com/"
  HEADER "Sec-Fetch-Dest: empty"
  HEADER "Sec-Fetch-Mode: cors"
  HEADER "Sec-Fetch-Site: same-site"
  HEADER "Connection: keep-alive"
(continuation HEADER/COOKIE lines are indented — send the whole request as ONE add_block call)
""",
        ["SCRIPT"] = """
BEGIN SCRIPT JavaScript
  // JS (Jint engine). Bot variables are readable as plain names — `ck`, `st` —
  // no <VAR> syntax needed inside script code.
  var x = 1;
END SCRIPT -> VARS "x"

// languages: JavaScript | IronPython
// send BEGIN..END as ONE add_block call (it's a multi-line construct, not a block line)
""",
        ["TRANSLATE"] = """
FUNCTION Translate
  KEY "US" VALUE "United States"
  KEY "BR" VALUE "Brazil"
  "<INPUT>" -> VAR "COUNTRY"
// dictionary find/replace, longest key first; input literal + -> VAR/CAP come LAST
// send as ONE add_block call
""",
        ["PARSE"] = """
PARSE "<SOURCE>" JSON "token" -> VAR "TOKEN"
PARSE "<SOURCE>" LR "csrf\":\"" "\"" -> VAR "CSRF"
PARSE "<SOURCE>" REGEX "user_id=(\\d+)" "$1" -> VAR "UID"
PARSE "<ADDRESS>" CSS "title" "innerText" -> VAR "TITLE"

// captured output (shows in hits, green in debugger):
PARSE "<SOURCE>" LR "\"firstName\" : \"" "\"" CreateEmpty=FALSE -> CAP "USER"
PARSE "<SOURCE>" JSON "profileName" Recursive=TRUE CreateEmpty=FALSE -> CAP "PROFILES"
""",
        ["KEYCHECK"] = """
// After a request — first matching KEYCHAIN wins, so order matters:
// put the MOST SPECIFIC chains first (ban/rate-limit before generic failure).
KEYCHECK BanOnToCheck=TRUE
  KEYCHAIN Ban OR
    KEY "<RESPONSECODE>" EqualTo "429"
    KEY "<RESPONSECODE>" EqualTo "403"
  KEYCHAIN Success OR
    KEY "<SOURCE>" Contains "welcome"
    KEY "<RESPONSECODE>" EqualTo "200"
  KEYCHAIN Failure OR
    KEY "<SOURCE>" Contains "invalid"
    KEY "<RESPONSECODE>" EqualTo "401"
// no chain matched -> ToCheck (or BAN when BanOnToCheck=TRUE)

// CE opener — gate on the cookie load BEFORE any request (matches WRONGPATH):
COOKIECONTAINER "site.com" "<COOKIEPATH>" -> SAVE "ck"
KEYCHECK
  KEYCHAIN Failure OR
    KEY "<ck>" EqualTo "WRONGPATH"
    KEY "<ck>" DoesNotContain "required_cookie_name"
  KEYCHAIN Success OR
    KEY "<ck>" Contains "required_cookie_name"

// Custom status chain (name shows as the hit's status):
  KEYCHAIN Custom "FREE_PLAN" OR
    KEY "<PLAN>" Contains "free"
""",
        ["FUNCTION"] = """
FUNCTION Hash MD5 "<PASS>" -> VAR "MD5PASS"
FUNCTION Base64Encode "<USER>:<PASS>" -> VAR "B64"
FUNCTION RandomString "?u?l?l?d?d?d" -> VAR "RAND"
FUNCTION Compute "1" "+" "1" -> VAR "TWO"
FUNCTION CurrentUnixTime -> VAR "NOW"
FUNCTION UnixTimeToDate "yyyy-MM-dd" "<NOW>" -> VAR "DATE"
FUNCTION Replace "_" "-" "<VAR>" -> VAR "VAR"
FUNCTION Constant "AUTHOR_NAME" -> CAP "CONFIG BY"

// Translate = multi-line dictionary (send as ONE add_block call):
FUNCTION Translate
  KEY "US" VALUE "United States"
  KEY "BR" VALUE "Brazil"
  "<CT>" -> CAP "COUNTRY"
""",
        ["UTILITY"] = """
UTILITY VAR "MYVAR" Split "," -> VAR "SPLIT" /LIST
UTILITY LIST "SPLIT" Length -> VAR "LEN"
UTILITY FILE "out.txt" Write "<SOURCE>"

// hit-export pattern (standard CE closer):
UTILITY Folder "SITENAME_<ti>" Create
UTILITY File "SITENAME_<ti>\\[<USER>].txt" WriteLines "USER: <USER> \\n CAP: <MYCAP> \\n\\n<COOKIENETSCAPE>"
""",
        ["COOKIECONTAINER"] = """
COOKIECONTAINER "<domainFilter>" "<COOKIEPATH>" -> SAVE "CK"
// FIRST literal = domain substring filter (e.g. "hbomax.com"), SECOND = cookie file path.
// WRONG! -> COOKIECONTAINER "<SOURCE>"   (that puts the source in the DOMAIN slot)
// -> SAVE "ck" stores "name: value" lines in <ck> and Netscape lines in <COOKIENETSCAPE>
// Always follow with a KEYCHECK for "WRONGPATH" (file unreadable).
""",
        ["TCP"] = "TCP Connect \"host\" \"443\" SSL\nTCP Send \"payload\" -> VAR \"RESP\"\nTCP Disconnect",
        ["NAVIGATE"] = "NAVIGATE \"https://example.com\" 30",
        ["EXECUTEJS"] = "EXECUTEJS \"return document.title;\" -> VAR \"TITLE\"",
        ["ELEMENTACTION"] = "ELEMENTACTION ID \"login-btn\" Click\nELEMENTACTION NAME \"username\" SendKeys \"<USER>\"",
        ["BYPASSCF"] = "BYPASSCF \"https://site.com\" UA \"<USERAGENT>\"",
        ["CURLREQUEST"] = """
// CurlTls plugin — TLS-fingerprinted request (defeats JA3/JA4 checks like Cloudflare)
CURLREQUEST "https://example.com" BROWSER "chrome120"
CURLREQUEST "https://api.com/login" BROWSER "chrome120" METHOD "POST" CONTENT "u=<USER>&p=<PASS>" CTYPE "application/x-www-form-urlencoded" HEADER "Accept: application/json" -> VAR "SOURCE"
""",
    };
}
