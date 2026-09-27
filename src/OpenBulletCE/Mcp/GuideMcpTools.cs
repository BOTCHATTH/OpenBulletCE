using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools serving guides: how to build configs, the tool surface, syntax.
/// </summary>
[McpServerToolType]
public sealed class GuideMcpTools
{
    /// <summary>Returns the full guide for working with this MCP server.</summary>
    [McpServerTool(Name = "get_mcp_guide"),
     Description("Returns the complete guide for using this MCP server: workflow for creating configs, editing scripts, debugging, running jobs, and managing hits. READ THIS FIRST if you're unsure where to start.")]
    public string GetMcpGuide()
    {
        return """
OC2 MCP GUIDE — OpenBullet Cookie 2
====================================
Engine: LoliScript (same as OpenBullet 1). Configs are .lce files in Configs/.
Variables use <NAME> interpolation, e.g. "<USER>", "<PASS>", "<SOURCE>".

TYPICAL WORKFLOW — make a config end-to-end
 1. get_environment_info        -> wordlist types, plugins, folders
 2. create_config name=...      -> creates an EMPTY .lce (no script param)
 3. Build the stack ONE BLOCK AT A TIME — this is the required path:
    a. list_available_blocks     -> block keywords
    b. get_block_template BLOCK  -> exact syntax line for that block type
    c. add_block "REQUEST GET \"url\"" -> each line is parse-checked instantly
    (write_config_script exists for full rewrites ONLY — never for building)
 4. set_config_settings         -> needsProxies, allowedWordlist1, suggestedBots...
 5. update_config_metadata      -> author, base64 image, version
 6. debug_config data="u:p"     -> run the real debugger, inspect variables + log
 7. validate_script             -> parse check
 8. create_job + start_job      -> run it for real; watch with get_job / get_job_log / get_job_hits

EDITING AN EXISTING CONFIG
 - get_script_map               -> numbered map (block index -> line)
 - get_block / get_script_region-> read exact lines
 - update_block / patch_script_region / add_block / remove_block / move_block -> edit
 - undo_last_change             -> revert last MCP edit
 - commit_stack                 -> save + revalidate

LOLI SCRIPT BASICS
 - One block per line: REQUEST GET "url" HEADER "A: b"
 - MULTI-LINE constructs (send as ONE add_block call, continuation lines INDENTED):
   REQUEST + HEADER/COOKIE lines, KEYCHECK + KEYCHAIN + KEY, FUNCTION Translate
   + KEY/VALUE dict, BEGIN SCRIPT..END SCRIPT
 - Disabled block: prefix with !  —  !REQUEST GET "url"
 - Label: prefix #LABEL           —  #LOGIN REQUEST POST ...
 - Comment lines: ## comment
 - Output: -> VAR "name" (working var) or -> CAP "name" (CAPTURED — appears in
   hits and the debugger's green captured vars; use CAP for every value the user
   wants in the hit record)
 - Script blocks: BEGIN SCRIPT JavaScript|IronPython ... END SCRIPT -> VARS "x"
   — Jint/IronPython; bot vars readable by bare name inside code
 - Data slices (from wordlist type): <USER> <PASS> / CE cookie type: <COOKIEPATH>
 - Bot data vars: <SOURCE> <ADDRESS> <RESPONSECODE> <HEADERS> <COOKIES> <STATUS>
 - Flow control: IF "a" EqualTo "b" .. ELSE .. ENDIF, WHILE..ENDWHILE, JUMP #label
 - Commands: SET VAR "x" "v", PRINT "text", DELETE VAR "x", SET PROXY "h:p"

PRO CONFIG ANATOMY (the pattern real .lce configs follow):
 1. ## header comment
 2. COOKIECONTAINER "site.com" "<COOKIEPATH>" -> SAVE "ck"  (domain filter FIRST)
 3. KEYCHECK gating on <ck> == WRONGPATH / missing required cookie -> Failure
 4. FUNCTION/SCRIPT preprocessing (timestamps, GUIDs, token decode) -> VAR
 5. REQUEST with a FULL browser header set (10+ headers — UA, Accept,
    Accept-Language, Accept-Encoding, Origin, Referer, sec-fetch-* ...)
 6. KEYCHECK — ban/rate-limit chains FIRST, then success, then failure
 7. PARSE ... CreateEmpty=FALSE -> CAP "NAME" for every hit field
 8. Repeat request+parse cycles for each API the site needs
 9. UTILITY Folder + UTILITY File WriteLines -> export hit detail file
    (include <COOKIENETSCAPE> to save the full cookie jar)
10. FUNCTION Constant "author" -> CAP "CONFIG BY"

SOURCES OF TRUTH
 - list_available_blocks / get_block_details  -> exact block syntax
 - get_block_template                         -> ready-to-paste multi-line templates
 - get_config_making_guide                    -> deeper config-building tutorial
 - analyze_har_file / generate_config_from_har-> turn a HAR capture into requests
 - get_cloudflare_guidance                    -> CF/TLS bypass playbook (CurlTls plugin)
""";
    }

    /// <summary>Returns the config-making tutorial, optionally one topic.</summary>
    [McpServerTool(Name = "get_config_making_guide"),
     Description("Returns the config-making tutorial for LoliScript configs. Topics: requests, parsing, keychecks, variables, cookies, selenium, proxies, debugging. Omit topic for the full guide.")]
    public string GetConfigMakingGuide(
        [Description("Optional topic: requests|parsing|keychecks|variables|cookies|selenium|proxies|debugging")] string topic = "")
    {
        if (string.IsNullOrWhiteSpace(topic)) return FullGuide;
        var key = topic.Trim().ToLowerInvariant();
        return Topics.TryGetValue(key, out var t) ? t : $"Unknown topic '{topic}'. Topics: {string.Join(", ", Topics.Keys)}";
    }

    private const string FullGuide = """
CONFIG MAKING — LOLISCRIPT (Cookie Edition)
===========================================

== DATA FLOW ==
Each bot gets one data line (from wordlist or cookie list). The wordlist type
slices it into variables: e.g. type "Default" sep ":" slices <USER>, <PASS>.
Blocks read/write <VARIABLES>. The block's KEYCHECK decides the final status.

== REQUESTS ==
REQUEST GET "https://site.com/login" HEADER "User-Agent: ..."
REQUEST POST "..." CONTENT "user=<USER>&pass=<PASS>" CONTENTTYPE "application/x-www-form-urlencoded"
After a request: <SOURCE> = body, <RESPONSECODE> = status, <ADDRESS> = final URL,
<COOKIES> = cookie dict, <HEADERS> = response headers.

== PARSING ==
PARSE "<SOURCE>" JSON "token" -> VAR "TOKEN"
PARSE "<SOURCE>" LR "left" "right" -> VAR "X"   (Recursive flag for all matches)
PARSE "<SOURCE>" REGEX "pat" "$1" -> VAR "Y"

== KEYCHECKS (deciding status) ==
KEYCHECK BanOnToCheck=TRUE
  KEYCHAIN Ban OR
    KEY "<RESPONSECODE>" EqualTo "429"
  KEYCHAIN Success OR
    KEY "<SOURCE>" Contains "welcome"
  KEYCHAIN Failure OR
    KEY "<RESPONSECODE>" EqualTo "401"
  KEYCHAIN Custom "FREE" OR
    KEY "<PLAN>" Contains "free"
First matching keychain wins — order chains most-specific-first (Ban/Custom
before generic Failure). No match -> ToCheck (or BAN if BanOnToCheck).
The whole KEYCHECK is ONE multi-line block: indented KEYCHAIN/KEY children.

== COOKIES (Cookie Edition) ==
Cookie lists are folders of Netscape-format cookie files; the CE wordlist type
gives <COOKIEPATH> = the file path. Standard opener:
  COOKIECONTAINER "site.com" "<COOKIEPATH>" -> SAVE "ck"
  KEYCHECK
    KEYCHAIN Failure OR
      KEY "<ck>" EqualTo "WRONGPATH"
      KEY "<ck>" DoesNotContain "required_cookie"
FIRST literal = domain substring filter, SECOND = file path.
-> SAVE "ck" stores "name: value" lines in <ck> and raw Netscape lines in
<COOKIENETSCAPE>. After requests, read session cookies via PARSE "<COOKIES(name)>".

== FUNCTIONS ==
FUNCTION Hash MD5 "<PASS>" -> VAR "H"
FUNCTION Base64Encode "x" -> VAR "B"
FUNCTION Base64Decode "<b64>" -> VAR "D"
FUNCTION RandomNum "1000" "9999" -> VAR "N"
FUNCTION CurrentUnixTime -> VAR "to"
FUNCTION UnixTimeToDate "yyyy-MM-dd" "<to>" -> VAR "ti"
FUNCTION Replace "_" "-" "<v>" -> VAR "v"
FUNCTION Constant "AUTHOR" -> CAP "CONFIG BY"
FUNCTION Translate     (multi-line dict: KEY "k" VALUE "v" lines, input+-> last)
FUNCTION Delay "500"

== SCRIPT BLOCKS ==
BEGIN SCRIPT JavaScript
  // Jint JS — read bot vars by bare name (ck, st...), build output vars
END SCRIPT -> VARS "out1,out2"
Same for IronPython. Send BEGIN..END as ONE add_block call.

== FLOW / COMMANDS ==
IF "<a>" EqualTo "b" ... ELSE ... ENDIF    WHILE "<c>" EqualTo "x" ... ENDWHILE
JUMP #label      SET VAR "x" "v" / SET PROXY "h:p" / PRINT "text" / DELETE VAR "x"

== SELENIUM ==
NAVIGATE "url" / ELEMENTACTION ... / EXECUTEJS "..." / BROWSERACTION.
Config settings: forceHeadless, alwaysOpen, alwaysQuit, quitOnBanRetry, randomUA.

== TIPS ==
- Prefix ! to disable a line instead of deleting.
- #LABEL lines let FUNCTION/BotActions jump.
- Always debug_config with a real data line before running a job.
""";

    private static readonly Dictionary<string, string> Topics = new()
    {
        ["requests"] = """
REQUESTS: REQUEST <METHOD> "url" then modifiers:
 CONTENT "body" (form), RAWDATA "body" (raw), CTYPE/CONTENTTYPE "type",
 HEADER "k: v" (repeatable), COOKIE "k: v", USERNAME/PASSWORD (basic auth).
After: <SOURCE> <RESPONSECODE> <ADDRESS> <HEADERS> <COOKIES>.
Prefer CURLREQUEST (CurlTls plugin) when the target checks TLS fingerprints.
""",
        ["parsing"] = """
PARSE "<SOURCE>" <TYPE> ... -> VAR|CAP "name"
 LR "l" "r" — between strings (add Recursive for lists)
 CSS "selector" "attribute|innerHTML|innerText"
 JSON "path.to.field" — supports [0] array index
 XPATH "xpath" "attr" — REGEX "pattern" "$1"
""",
        ["keychecks"] = """
KEYCHECK [BanOn4XX=TRUE|FALSE] [BanOnToCheck=TRUE|FALSE]
  KEYCHAIN <Success|Failure|Ban|Retry|Custom "N"> <OR|AND>
    KEY "left" <COMPARER> ["right"]
Comparers: EqualTo NotEqualTo Contains NotContains ContainsAny ContainsAll
 GreaterThan LessThan Exists DoesNotExist MatchesRegex ContainsRegex.
First matching chain wins; no match = ToCheck.
""",
        ["variables"] = """
Data slices: <USER> <PASS> (per wordlist type).
Bot data: <SOURCE> <ADDRESS> <RESPONSECODE> <HEADERS> <COOKIES> <STATUS> <PROXY>.
Set vars with -> VAR "name"; capture with -> CAP "name" (shows in hits).
Inside BEGIN SCRIPT blocks use JS/Python syntax and <VARS> interpolation.
""",
        ["cookies"] = """
Cookie Edition: data source is a cookie LIST — a folder of Netscape-format
cookie files. The CE wordlist type slices the data line to <COOKIEPATH> (the
file's path). Standard opener:

  COOKIECONTAINER "site.com" "<COOKIEPATH>" -> SAVE "ck"

FIRST literal is a DOMAIN substring filter (only matching cookie lines load),
SECOND is the file path. If the file can't be read -> <ck> = "WRONGPATH"
(always KEYCHECK it right after). -> SAVE "ck" also fills <COOKIENETSCAPE>
with the raw Netscape lines — use it in UTILITY File WriteLines to export the
full jar with each hit. Read live session cookies via PARSE "<COOKIES(name)>".
""",
        ["selenium"] = """
NAVIGATE "url" [timeout] — open page.
ELEMENTACTION <ID|CLASS|SELECTOR|XPATH> "sel" <Click|SendKeys "x"|GetText|GetAttribute "a"|Submit|Wait>
EXECUTEJS "code" -> VAR "x" — run JS.
BROWSERACTION <Open|Close|Refresh|Back|Forward|SwitchToTab n>
Config flags: forceHeadless, alwaysOpen, alwaysQuit, quitOnBanRetry, randomUA.
""",
        ["proxies"] = """
Job-level: create_job proxyMode = Default|On|Off. Config needsProxies must be set
for On. Per-request the bot uses its assigned proxy automatically.
In-script proxy switch: SET PROXY "host:port" (SetParser).
Runner proxy pool stats visible via get_job.
""",
        ["debugging"] = """
debug_config name data="..." [proxy] — runs the whole script on one line in a real
bot. Returns status, all variables, captures, log. quick_test_config runs ad-hoc
script without a config. Inspect log lines to find the failing block, fix with
update_block / patch_script_region, re-run.
""",
    };
}
