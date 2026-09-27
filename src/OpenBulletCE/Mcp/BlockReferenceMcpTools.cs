using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using RuriLib.LS;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools exposing the LoliScript block reference — syntax for every block
/// type available in this build (built-ins + plugins).
/// </summary>
[McpServerToolType]
public sealed class BlockReferenceMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Lists every block keyword the parser understands.</summary>
    [McpServerTool(Name = "list_available_blocks"),
     Description("Lists every block type the LoliScript parser understands (built-ins and loaded plugins), plus non-block script constructs (SCRIPT, IF/WHILE, commands). Use get_block_details or get_block_template for syntax of a specific one.")]
    public string ListAvailableBlocks()
    {
        var builtins = BlockParser.BlockMappings.Keys.OrderBy(k => k).Select(k => new
        {
            keyword = k,
            description = BlockPurpose.TryGetValue(k, out var d) ? d : "(no description)"
        });
        var plugins = OB.BlockPlugins.Select(p => p.Name);
        return JsonSerializer.Serialize(new
        {
            builtinBlocks = builtins,
            pluginBlocks = plugins,
            scriptConstructs = new[]
            {
                "BEGIN SCRIPT JavaScript|IronPython ... END SCRIPT -> VARS \"name\"  (multi-line code block, NOT a parser block — add_block accepts it as one call)",
                "IF \"a\" <COMPARER> \"b\" ... ELSE ... ENDIF / WHILE ... ENDWHILE / JUMP #label  (flow control)",
                "SET VAR \"x\" \"val\" | SET PROXY \"host:port\" | PRINT \"text\" | DELETE VAR \"x\"  (commands)",
                "## comment  /  #LABEL <block>  /  !<block> disabled"
            },
            note = "Multi-line constructs (KEYCHECK+KEYCHAINs, FUNCTION Translate dict, BEGIN SCRIPT) must be sent to add_block as ONE call containing the newlines — never line-by-line."
        }, JsonOpts);
    }

    private static readonly Dictionary<string, string> BlockPurpose = new(StringComparer.OrdinalIgnoreCase)
    {
        ["REQUEST"] = "HTTP request + response into <SOURCE>/<RESPONSECODE>/<ADDRESS>/<COOKIES>",
        ["PARSE"] = "extract text/JSON/regex/CSS from a var -> VAR (working) or CAP (captured, shows in hits)",
        ["KEYCHECK"] = "decide bot status (Success/Failure/Ban/Retry/Custom) via KEYCHAIN/KEY conditions",
        ["FUNCTION"] = "string/crypto/time/dict-transform functions -> VAR|CAP",
        ["UTILITY"] = "file/folder/list/var/convert operations incl. writing hit files",
        ["COOKIECONTAINER"] = "load a Netscape cookie file for a domain into the session (CE core block)",
        ["TCP"] = "raw TCP connect/send/disconnect",
        ["CAPTCHA"] = "image captcha OCR", ["RECAPTCHA"] = "reCAPTCHA v2 solver",
        ["SOLVECAPTCHA"] = "generic captcha service", ["REPORTCAPTCHA"] = "report bad captcha solve",
        ["BYPASSCF"] = "Cloudflare IUAM bypass via Selenium",
        ["NAVIGATE"] = "Selenium: open URL", ["BROWSERACTION"] = "Selenium: browser control",
        ["ELEMENTACTION"] = "Selenium: click/type/read DOM element", ["EXECUTEJS"] = "Selenium: run JS",
    };

    /// <summary>Returns LoliScript syntax reference for one block type.</summary>
    [McpServerTool(Name = "get_block_details"),
     Description("Returns LoliScript syntax, parameters and examples for a block type (e.g. REQUEST, PARSE, KEYCHECK, FUNCTION, UTILITY). Call list_available_blocks for the full list.")]
    public string GetBlockDetails(
        [Description("Block type keyword (e.g. REQUEST, PARSE, KEYCHECK)")] string blockType)
    {
        var key = blockType.Trim().ToUpperInvariant();
        if (BlockDocs.TryGetValue(key, out var doc)) return doc;

        if (BlockParser.BlockMappings.TryGetValue(key, out _))
            return $"{key} is a registered block (possibly a plugin) but has no written reference. Check its plugin docs; parsing follows standard LoliScript rules: 'BLOCKNAME \"args\" -> CAPTURE \"var\"', prefix with ! to disable, #LABEL to label.";

        return $"Unknown block '{blockType}'. Use list_available_blocks.";
    }

    internal static readonly Dictionary<string, string> BlockDocs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["REQUEST"] = """
REQUEST — performs an HTTP request. Syntax:
  REQUEST METHOD "url" [MULTIPART|BASICAUTH|STANDARD|RAW] [payload options] [modifier options]

Methods: GET POST PUT PATCH DELETE HEAD OPTIONS TRACE CONNECT

Content types (pick one):
  STANDARD    -> PostData follows: CONTENT "a=1&b=2"
  RAW         -> RAWDATA "raw body"
  MULTIPART   -> boundaries & parts (see below)
  BASICAUTH   -> USERNAME "u" PASSWORD "p"

Options (repeatable):
  CONTENT "postdata"            string content for STANDARD
  RAWDATA "postdata"            raw body for RAW
  STRINGCONTENT "name:value"    multipart text part
  FILECONTENT "name:file:ctype" multipart file part
  COOKIE "name:value"           add request cookie
  HEADER "name:value"           add request header
  CONTENTTYPE "type"            e.g. "application/json"
  USERNAME "x" PASSWORD "x"     basic auth
  ALWAYSREADCONTENT             read the body even on error codes
  AUTOHDR / AUTOLOCATION        auto headers / redirect handling
  SECPROTO "Tls12"              security protocol

Examples:
  REQUEST GET "https://site.com/login"
  REQUEST POST "https://site.com/auth" CONTENT "user=<USER>&pass=<PASS>" CONTENTTYPE "application/x-www-form-urlencoded"
  REQUEST POST "https://api.com/x" RAWDATA "{\"a\":1}" CONTENTTYPE "application/json" HEADER "Authorization: Bearer <TOKEN>"

Note: use CURLREQUEST (CurlTls plugin) instead when the target has TLS fingerprinting (Cloudflare etc.).
""",
        ["PARSE"] = """
PARSE — extracts data from a variable (usually SOURCE). Syntax:
  PARSE "<SOURCE>" TYPE ... -> VAR "name" [/LIST|SINGLE|DICT] [PREFIX "x"] [SUFFIX "y"]

Types:
  LR "left" "right"                 text between strings, e.g. PARSE "<SOURCE>" LR "name\":\"v", "\"" -> VAR "token" — Recursive/Regex flags allowed
  CSS "selector" "attribute"        HTML element attribute. Use attribute "innerHTML"/"innerText" for text. [INDEX n]
  JSON "field"                    JSON path e.g. "data.token" or "[0].id"
  XPATH "xpath" "attribute"       XPath attribute extraction
  REGEX "pattern" "output"        regex match -> output with $1 groups, e.g. REGEX "token=(\\w+)" "$1"

Flags:
  Recursive[=TRUE|FALSE]  get all matches into a list
  CreateEmpty[=TRUE|FALSE] FALSE = on no-match don't create the var (lets you
                            test Exists/DoesNotExist in KEYCHECKs)

Output: -> VAR "name" (working var) | -> CAP "name" (CAPTURED — shown in hits,
green in the debugger Variables tab). Use CAP for anything you want saved:
PARSE "<SOURCE>" JSON "profileName" Recursive=TRUE CreateEmpty=FALSE -> CAP "PROFILES"

Examples:
  PARSE "<SOURCE>" JSON "access_token" -> VAR "TOKEN"
  PARSE "<SOURCE>" LR "\"firstName\" : \"" "\"" CreateEmpty=FALSE -> CAP "USER"
  PARSE "<ADDRESS>" LR "title>" "</title" -> VAR "TITLE"
  PARSE "<COOKIES(*)>" ... -> VAR "x" (cookie dictionary)
""",
        ["KEYCHECK"] = """
KEYCHECK — sets the bot status by testing conditions. Syntax:
  KEYCHECK [BanOn4XX=TRUE|FALSE] [BanOnToCheck=TRUE|FALSE]
    KEYCHAIN <TYPE> <MODE>
      KEY "leftTerm" <COMPARER> ["rightTerm"]

TYPE: Success | Failure | Ban | Retry | Custom "NAME"
MODE: OR (any key matches) | AND (all must match)
COMPARER: EqualTo NotEqualTo Contains NotContains ContainsAny ContainsAll
          ContainsRegex MatchesRegex GreaterThan LessThan Exists DoesNotExist
Short key form: KEY "needle" alone means "<SOURCE>" Contains "needle".

Example:
  KEYCHECK BanOnToCheck=TRUE
    KEYCHAIN Success OR
      KEY "<SOURCE>" Contains "welcome"
      KEY "<RESPONSECODE>" EqualTo "200"
    KEYCHAIN Failure OR
      KEY "invalid"
""",
        ["FUNCTION"] = """
FUNCTION — string/crypto/utility function. Syntax:
  FUNCTION <NAME> [args] -> VAR "out"

Names & args:
  Constant "value"
  Base64Encode "input" | Base64Decode "input"
  Hash <MD5|SHA1|SHA256|SHA384|SHA512> "input"
  HMAC <hashtype> "key" "input"
  Translate — dictionary replace (multi-line!): FUNCTION Translate then indented
    KEY "k" VALUE "v" lines, then the INPUT literal + -> VAR|CAP last. See get_block_details TRANSLATE.
  DateToUnixTime "format" "input" | UnixTimeToDate "format" "input" | UnixTimeToISO8601 "input" | CurrentUnixTime
  Constant "value" -> CAP "x" — a captured constant (e.g. config author credit)
  Length "input" | ToLowercase "input" | ToUppercase "input"
  Replace "what" "with" "input"
  RegexMatch "pattern" "input"
  URLEncode "input" | URLDecode "input" | Unescape "input"
  HTMLEntityEncode/HTMLEntityDecode "input"
  RandomNum "min" "max" | RandomString "mask" (mask chars: ?l lower ?u upper ?d digit ?s symbol ?h hex ?a alnum)
  Ceil/Floor/Round "input"
  Compute "a" <+|-|*|/|%> "b"
  CountOccurrences "needle" "haystack"
  ClearCookies
  RSAEncrypt "modulus" "exponent" "input" | RSAPKCS1PAD2 "modulus" "exponent" "input"
  Delay "milliseconds"
  CharAt "index" "input" | Substring "index" "length" "input"

Example: FUNCTION Hash MD5 "<PASS>" -> VAR "MD5PASS"
""",
        ["UTILITY"] = """
UTILITY — variable/list/file/folder/convert operations. Syntax:
  UTILITY <GROUP> ...

Groups:
  LIST "listName" <ACTION> ...
    Actions: Join "sep" | Length | Sort | Shuffle | RemoveDuplicates | Remove "item" | Add "item" | Insert "item" "index" | IndexOf "item"
  VAR "varName" <ACTION>: Split "sep" | Delete
  CONVERT <from> <to> "input"   e.g. CONVERT BASE64 HEX "<SOURCE>"
  FILE "path" <ACTION>          Write/Append/WriteLines/Read/ReadLines/Delete/Exists/Copy/Move/CreateZip...
  FOLDER "path" <ACTION>        Create/Delete/Exists
  RESULT <data> <captured>      push a fake hit

Hit-export pattern (standard CE closer — folder per day, file per hit, full cookie dump):
  FUNCTION CurrentUnixTime -> VAR "to"
  FUNCTION UnixTimeToDate "yyyy-MM-dd" "<to>" -> VAR "ti"
  UTILITY Folder "MYFOLDER_<ti>" Create
  UTILITY File "MYFOLDER_<ti>\\[<USER>].txt" WriteLines "DATA: <USER> \\n CAPS: <CAP> \\n <COOKIENETSCAPE>"
(WriteLines uses \\n inside the literal for line breaks; <COOKIENETSCAPE> comes from
COOKIECONTAINER ... -> SAVE.)

Then -> VAR "name" for producing ops.
""",
        ["COOKIECONTAINER"] = """
COOKIECONTAINER — loads cookies from a Netscape-format cookie file into the bot session (Cookie Edition core block).
Syntax:
  COOKIECONTAINER "<domainFilter>" "<filePath>" -> SAVE|NOTSAVE "<varName>"

- <domainFilter>: only cookie lines whose domain CONTAINS this string are loaded
  (e.g. "hbomax.com" loads only hbomax.com cookies; use the site's registrable domain).
- <filePath>: typically "<COOKIEPATH>" (the CE wordlist slice = path to the cookie file).
- -> SAVE "var": stores the loaded cookies as "name: value" lines in <var> AND
  stores the raw Netscape lines in <COOKIENETSCAPE> (use it later in UTILITY File WriteLines
  to export the full cookie file into a hit record).
- -> NOTSAVE: loads session only.

If the file can't be read the output var is set to "WRONGPATH" — check it in a KEYCHECK
right after this block (that's the standard CE opener, see get_config_making_guide).

Example:
  COOKIECONTAINER "hbomax.com" "<COOKIEPATH>" -> SAVE "ck"

  KEYCHECK
    KEYCHAIN Failure OR
      KEY "<ck>" EqualTo "WRONGPATH"
      KEY "<ck>" DoesNotContain "st:"
""",
        ["SCRIPT"] = """
BEGIN SCRIPT / END SCRIPT — multi-line embedded code (NOT a single-line block).
  BEGIN SCRIPT JavaScript
    // JS (Jint). Bot variables are readable as plain names: use `ck`, `st`, etc.
    // Assign output by building a var, then export via -> VARS "name1,name2"
  END SCRIPT -> VARS "out"
Languages: JavaScript | IronPython
Add via add_block as ONE call with all lines (or patch_script_region / write_config_script).
Inside the script lines are code — no LoliScript interpolation, access vars by bare name.
""",
        ["TRANSLATE"] = """
FUNCTION Translate — dictionary-based find/replace, LONGEST key first (order-independent).
Syntax:
  FUNCTION Translate
    KEY "US" VALUE "United States"
    KEY "BR" VALUE "Brazil"
    ... more KEY/VALUE lines ...
    "<input>" -> VAR|CAP "out"
Matching keys inside <input> are replaced by their VALUEs. Multi-line block — send as ONE add_block call.
""",
        ["TCP"] = """
TCP — raw TCP client ops.
  TCP Connect "host" "port" [SSL]
  TCP Send "message" [-> VAR "response"]
  TCP Disconnect
""",
        ["NAVIGATE"] = "NAVIGATE \"url\" [timeoutSeconds] — Selenium: open a page.",
        ["BROWSERACTION"] = "BROWSERACTION <Open|Close|SwitchToTab|Refresh|Back|Forward|...> — Selenium browser control.",
        ["ELEMENTACTION"] = "ELEMENTACTION <ID|CLASS|SELECTOR|XPATH> \"selector\" <ACTION> [args] — click, type, get text/attribute on a DOM element.",
        ["EXECUTEJS"] = "EXECUTEJS \"javascript code\" [-> VAR \"name\"] — run JS in the Selenium browser.",
        ["CAPTCHA"] = "CAPTCHA \"imageUrl\" [UA \"ua\"] -> VAR \"answer\" — image captcha solving via OCR.",
        ["RECAPTCHA"] = "RECAPTCHA \"url\" \"siteKey\" -> VAR \"token\" — reCAPTCHA v2 solving via configured service.",
        ["SOLVECAPTCHA"] = "SOLVECAPTCHA <TYPE> ... — solve captcha via service (ImageText, Recaptcha, RecaptchaV3, FunCaptcha, HCaptcha, KeyCaptcha...).",
        ["REPORTCAPTCHA"] = "REPORTCAPTCHA <TYPE> \"captchaId\" — report a wrongly solved captcha to the service.",
        ["BYPASSCF"] = "BYPASSCF \"url\" [UA \"useragent\"] [SECPROTO <protocol>] — bypass Cloudflare IUAM challenge via Selenium; sets cf_clearance cookie.",
    };
}
