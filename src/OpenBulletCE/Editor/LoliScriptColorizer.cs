using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System.Text.RegularExpressions;

namespace OpenBulletCE.Editor;

/// <summary>
/// Syntax highlighting for LoliScript — colors block commands orange, strings green,
/// variables teal, keychain logic red/magenta, parameters cyan, comments muted.
/// </summary>
public class LoliScriptColorizer : DocumentColorizingTransformer
{
    private static readonly IBrush CommentBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#6A9955"));
    private static readonly IBrush StringBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#5BD75B"));
    private static readonly IBrush VariableBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#4EC9B0"));
    private static readonly IBrush CommandBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#FFA500"));
    private static readonly IBrush CheckBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#FF5370"));
    private static readonly IBrush ParamBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#9CDCFE"));
    private static readonly IBrush ArrowBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#FF79C6"));
    private static readonly IBrush LabelBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#C586C0"));
    private static readonly IBrush NumberBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#B5CEA8"));

    private const string Commands =
        @"REQUEST|HEADER|COOKIE|COOKIES|PARSE|KEYCHECK|FUNCTION|UTILITY|NAVIGATE|BROWSERACTION|ELEMENTACTION|EXECUTEJS|" +
        @"SOLVECAPTCHA|REPORTCAPTCHA|BYPASSCF|IMAGECAPTCHA|RECAPTCHA|TCP|SHELL|TRANSLATE|BYPASS|CAPTURE|" +
        @"SET|UNSET|DELETE|CLEAR|BAN|RETRY|CUSTOM|LOCK|TAKEONE|JUMP|GOTO|" +
        @"IF|ELSE|ENDIF|WHILE|ENDWHILE|FOR|ENDFOR|FOREACH|ENDFOREACH|TRY|CATCH|ENDTRY|" +
        @"BEGIN|END|SCRIPT|CSHARP|LUA|IRONPYTHON|JAVASCRIPT|SHELLCOMMAND|BLOCK|ENDBLOCK|DEFINE|VAR|CAP";

    private const string Checks =
        @"KEYCHAIN|STRINGKEY|LISTKEY|BOOLKEY|DICTKEY|FLOATKEY|INTKEY|BYTEKEY|SUCCESS|FAIL|NONE|ERROR|RETRYBAN|TOCHECK|" +
        @"OR|AND|Contains|DoesNotContain|EqualTo|NotEqualTo|GreaterThan|LessThan|MatchesRegex|DoesNotMatch|" +
        @"Exists|DoesNotExist|ExistsIn|IsBetween|IsNotBetween|IsNull|IsNotNull|IsEmpty|IsNotEmpty";

    private const string Params =
        @"GET|POST|PUT|PATCH|DELETE|HEAD|TRACE|CONNECT|OPTIONS|" +
        @"STANDARD|RAW|MULTIPART|BASICAUTH|CONTENT|RAWDATA|STRINGCONTENT|FILECONTENT|STRING|FILE|DATA|" +
        @"TYPE|MODE|HTTPLIB|BROWSER|BASE64|HEX|URL|ADDRESS|SOURCETYPE|RESPONSE|" +
        @"TRUE|FALSE|SystemNet|RuriLibHttp|CurlImpersonate|LR|CS|JSON|REGEX|CSS|XPATH|INDEX|RECURSIVE|" +
        @"Safe|Unsafe|AlwaysCreate|Input|Output|LeftDelim|RightDelim|CASE|SENSITIVE|INSENSITIVE|BINARY|" +
        @"AUTH|USER|PASS|PROXY|NOPROXY|AutoRedirect|ReadResponseSource|SaveAsScreenshot";

    private static readonly Regex Rx = new(
        @"(?<comment>//.*|##.*)" +
        @"|(?<str>""(?:\\.|[^""\\])*"")" +
        @"|(?<var>@[A-Za-z_][\w.]*|<[A-Za-z_][\w.]*>)" +
        @"|(?<label>#[A-Za-z_]\w*)" +
        @"|(?<arrow>=\>|->|=>)" +
        @"|(?<cmd>\b(?:" + Commands + @")\b)" +
        @"|(?<chk>\b(?:" + Checks + @")\b)" +
        @"|(?<prm>\b(?:" + Params + @")\b)" +
        @"|(?<num>\b\d+(?:\.\d+)?\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        if (string.IsNullOrEmpty(text)) return;

        foreach (Match m in Rx.Matches(text))
        {
            var brush =
                m.Groups["comment"].Success ? CommentBrush :
                m.Groups["str"].Success ? StringBrush :
                m.Groups["var"].Success ? VariableBrush :
                m.Groups["label"].Success ? LabelBrush :
                m.Groups["arrow"].Success ? ArrowBrush :
                m.Groups["cmd"].Success ? CommandBrush :
                m.Groups["chk"].Success ? CheckBrush :
                m.Groups["num"].Success ? NumberBrush :
                ParamBrush;

            ChangeLinePart(line.Offset + m.Index, line.Offset + m.Index + m.Length,
                e => e.TextRunProperties.SetForegroundBrush(brush));
        }
    }
}
