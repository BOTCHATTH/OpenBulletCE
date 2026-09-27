using Avalonia.Controls;
using Avalonia.Layout;
using RuriLib;

namespace OpenBulletCE.Views.StackerBlocks;

public class PageBlockParse : BlockPage
{
    private readonly BlockParse _b;
    private readonly StackPanel _lrPanel = new() { Spacing = 2 };
    private readonly StackPanel _cssPanel = new() { Spacing = 2 };
    private readonly StackPanel _jsonPanel = new() { Spacing = 2 };
    private readonly StackPanel _regexPanel = new() { Spacing = 2 };

    public PageBlockParse(BlockParse block) : base(block)
    {
        _b = block;

        Panel.Children.Add(Row("Parse:", Txt(nameof(BlockParse.ParseTarget)), 60));

        var capRow = new DockPanel();
        capRow.Children.Add(Check("Is Capture", nameof(BlockParse.IsCapture)));
        capRow.Children.Add(new TextBlock { Text = "  Var/Cap Name:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(10, 0, 5, 0) });
        capRow.Children.Add(Txt(nameof(BlockParse.VariableName)));
        Panel.Children.Add(capRow);

        var pf = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Avalonia.Thickness(0, 4) };
        pf.Children.Add(Row("Prefix:", Txt(nameof(BlockParse.Prefix)), 55));
        var sfx = Row("Suffix:", Txt(nameof(BlockParse.Suffix)), 55);
        Grid.SetColumn(sfx, 1);
        pf.Children.Add(sfx);
        Panel.Children.Add(pf);

        // Mode radios
        var modeRow = new DockPanel();
        modeRow.Children.Add(new TextBlock { Text = "Mode:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        foreach (var (label, pt) in new (string, ParseType)[] { ("LR", ParseType.LR), ("CSS", ParseType.CSS), ("JSON", ParseType.JSON), ("REGEX", ParseType.REGEX) })
        {
            var rb = new RadioButton { Content = label, IsChecked = _b.Type == pt, Margin = new Avalonia.Thickness(4, 0), VerticalAlignment = VerticalAlignment.Center };
            var captured = pt;
            rb.IsCheckedChanged += (s, e) => { if (rb.IsChecked == true) { _b.Type = captured; UpdatePanels(); } };
            DockPanel.SetDock(rb, Dock.Left);
            modeRow.Children.Add(rb);
        }
        Panel.Children.Add(modeRow);

        // LR
        _lrPanel.Children.Add(Row("Left string:", Txt(nameof(BlockParse.LeftString))));
        _lrPanel.Children.Add(Row("Right string:", Txt(nameof(BlockParse.RightString))));
        _lrPanel.Children.Add(WrapChecks(
            Check("Recursive", nameof(BlockParse.Recursive)),
            Check("Enc. Output", nameof(BlockParse.EncodeOutput)),
            Check("Create Empty", nameof(BlockParse.CreateEmpty)),
            Check("Use Regex", nameof(BlockParse.UseRegexLR), "Whether the program matches the LEFT and RIGHT strings through a regex pattern or with simple string in string searches")));
        _lrPanel.Children.Add(new TextBlock { Text = "Automatic mode:", Margin = new Avalonia.Thickness(0, 4, 0, 0) });
        _lrPanel.Children.Add(MultiBox("", null, 120, "You can paste the source code in this box, and highlight the part you want to parse."));

        // CSS
        _cssPanel.Children.Add(Row("Selector:", Txt(nameof(BlockParse.CssSelector))));
        _cssPanel.Children.Add(Row("Attribute:", Txt(nameof(BlockParse.AttributeName), "If you want to parse stuff directly inside the element, write innerHTML")));
        _cssPanel.Children.Add(Row("Index:", Num(nameof(BlockParse.CssElementIndex), 0, 1000)));
        _cssPanel.Children.Add(WrapChecks(
            Check("Recursive", nameof(BlockParse.Recursive)),
            Check("Encode Output", nameof(BlockParse.EncodeOutput)),
            Check("Create Empty", nameof(BlockParse.CreateEmpty))));

        // JSON
        _jsonPanel.Children.Add(Row("Field Name:", Txt(nameof(BlockParse.JsonField))));
        _jsonPanel.Children.Add(Check("Use JToken Parsing", nameof(BlockParse.JTokenParsing), "Allows navigating through fields using . for children and [i] or [*] for arrays"));
        _jsonPanel.Children.Add(WrapChecks(
            Check("Recursive", nameof(BlockParse.Recursive)),
            Check("Encode Output", nameof(BlockParse.EncodeOutput)),
            Check("Create Empty", nameof(BlockParse.CreateEmpty))));

        // REGEX
        _regexPanel.Children.Add(Row("Regex:", Txt(nameof(BlockParse.RegexString))));
        _regexPanel.Children.Add(Row("Output:", Txt(nameof(BlockParse.RegexOutput), "You can write: [0] = Full Match, [1] = First group etc.")));
        _regexPanel.Children.Add(WrapChecks(
            Check("Recursive", nameof(BlockParse.Recursive)),
            Check("Encode Output", nameof(BlockParse.EncodeOutput)),
            Check("Create Empty", nameof(BlockParse.CreateEmpty))));
        _regexPanel.Children.Add(WrapChecks(
            Check("Dot Matches Line Breaks", nameof(BlockParse.DotMatches)),
            Check("Case Sensitive", nameof(BlockParse.CaseSensitive))));

        Panel.Children.Add(_lrPanel);
        Panel.Children.Add(_cssPanel);
        Panel.Children.Add(_jsonPanel);
        Panel.Children.Add(_regexPanel);

        UpdatePanels();
    }

    private void UpdatePanels()
    {
        _lrPanel.IsVisible = _b.Type == ParseType.LR;
        _cssPanel.IsVisible = _b.Type == ParseType.CSS;
        _jsonPanel.IsVisible = _b.Type == ParseType.JSON;
        _regexPanel.IsVisible = _b.Type == ParseType.REGEX;
    }

    private static StackPanel WrapChecks(params Control[] checks)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var c in checks) p.Children.Add(c);
        return p;
    }
}
