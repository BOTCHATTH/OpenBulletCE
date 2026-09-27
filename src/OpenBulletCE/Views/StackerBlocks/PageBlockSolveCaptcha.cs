using Avalonia.Controls;
using Avalonia.Layout;
using CaptchaSharp.Enums;
using RuriLib;

namespace OpenBulletCE.Views.StackerBlocks;

public class PageBlockSolveCaptcha : BlockPage
{
    private readonly BlockSolveCaptcha _b;
    private readonly StackPanel _text = new() { Spacing = 4 };
    private readonly StackPanel _image = new() { Spacing = 4 };
    private readonly StackPanel _token = new() { Spacing = 4 };

    public PageBlockSolveCaptcha(BlockSolveCaptcha b) : base(b)
    {
        _b = b;

        var typeRow = new DockPanel();
        typeRow.Children.Add(new TextBlock { Text = "Type:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        var typeCombo = new ComboBox();
        foreach (var t in System.Enum.GetValues<CaptchaType>()) typeCombo.Items.Add(t);
        typeCombo.SelectedItem = _b.Type;
        typeCombo.SelectionChanged += (s, e) => { if (typeCombo.SelectedItem is CaptchaType t) { _b.Type = t; UpdatePanels(); } };
        typeRow.Children.Add(typeCombo);
        Panel.Children.Add(typeRow);

        Panel.Children.Add(Check("Send proxy to solver service to use for token based captchas", nameof(BlockSolveCaptcha.UseProxy)));
        Panel.Children.Add(Row("User Agent:", Txt(nameof(BlockSolveCaptcha.UserAgent))));

        // Text tab
        _text.Children.Add(Row("Question:", Txt(nameof(BlockSolveCaptcha.Question))));
        _text.Children.Add(Row("Language Group:", EnumCombo<CaptchaLanguageGroup>(_b.LanguageGroup, v => _b.LanguageGroup = v), 90));
        _text.Children.Add(Row("Language:", EnumCombo<CaptchaLanguage>(_b.Language, v => _b.Language = v), 60));
        Panel.Children.Add(_text);

        // Image tab
        _image.Children.Add(Row("Base64:", Txt(nameof(BlockSolveCaptcha.Base64))));
        _image.Children.Add(Row("Language Group:", EnumCombo<CaptchaLanguageGroup>(_b.LanguageGroup, v => _b.LanguageGroup = v), 90));
        _image.Children.Add(Row("Language:", EnumCombo<CaptchaLanguage>(_b.Language, v => _b.Language = v), 60));
        _image.Children.Add(Wrap(
            Check("Is Phrase", nameof(BlockSolveCaptcha.IsPhrase)),
            Check("Case Sensitive", nameof(BlockSolveCaptcha.CaseSensitive)),
            Check("Requires Calculation", nameof(BlockSolveCaptcha.RequiresCalculation))));
        var lenRow = new DockPanel();
        lenRow.Children.Add(Row("Min Length:", Num(nameof(BlockSolveCaptcha.MinLength), 0, 100), 80));
        lenRow.Children.Add(Row("Max Length:", Num(nameof(BlockSolveCaptcha.MaxLength), 0, 100), 80));
        _image.Children.Add(lenRow);
        _image.Children.Add(Row("Charset:", EnumCombo<CharacterSet>(_b.CharSet, v => _b.CharSet = v), 55));
        _image.Children.Add(Row("Text Instructions:", Txt(nameof(BlockSolveCaptcha.TextInstructions))));
        Panel.Children.Add(_image);

        // Token tab (ReCaptcha/HCaptcha/FunCaptcha/GeeTest/Capy/KeyCaptcha)
        _token.Children.Add(Row("Site Key:", Txt(nameof(BlockSolveCaptcha.SiteKey))));
        _token.Children.Add(Row("Site URL:", Txt(nameof(BlockSolveCaptcha.SiteUrl))));
        _token.Children.Add(Wrap(
            Check("Is Invisible", nameof(BlockSolveCaptcha.IsInvisible)),
            Check("NoJS", nameof(BlockSolveCaptcha.NoJS))));
        _token.Children.Add(Row("Action:", Txt(nameof(BlockSolveCaptcha.Action))));
        _token.Children.Add(Row("Min Score:", Txt(nameof(BlockSolveCaptcha.MinScore))));
        _token.Children.Add(Row("Public Key:", Txt(nameof(BlockSolveCaptcha.PublicKey))));
        _token.Children.Add(Row("Service URL:", Txt(nameof(BlockSolveCaptcha.ServiceUrl))));
        _token.Children.Add(Row("User ID:", Txt(nameof(BlockSolveCaptcha.UserId))));
        _token.Children.Add(Row("Session ID:", Txt(nameof(BlockSolveCaptcha.SessionId))));
        _token.Children.Add(Row("WebServerSign1:", Txt(nameof(BlockSolveCaptcha.WebServerSign1))));
        _token.Children.Add(Row("WebServerSign2:", Txt(nameof(BlockSolveCaptcha.WebServerSign2))));
        _token.Children.Add(Row("GT:", Txt(nameof(BlockSolveCaptcha.GT))));
        _token.Children.Add(Row("Challenge:", Txt(nameof(BlockSolveCaptcha.Challenge))));
        _token.Children.Add(Row("API Server:", Txt(nameof(BlockSolveCaptcha.ApiServer))));
        Panel.Children.Add(_token);

        UpdatePanels();
    }

    private static StackPanel Wrap(params Control[] c)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var x in c) p.Children.Add(x);
        return p;
    }

    private void UpdatePanels()
    {
        _text.IsVisible = _b.Type == CaptchaType.TextCaptcha;
        _image.IsVisible = _b.Type == CaptchaType.ImageCaptcha;
        _token.IsVisible = _b.Type != CaptchaType.TextCaptcha && _b.Type != CaptchaType.ImageCaptcha;
    }
}

public class PageBlockRecaptcha : BlockPage
{
    public PageBlockRecaptcha(BlockRecaptcha b) : base(b)
    {
        Panel.Children.Add(Row("Variable Name:", Txt(nameof(BlockRecaptcha.VariableName))));
        Panel.Children.Add(Row("Page URL:", Txt(nameof(BlockRecaptcha.Url))));
        Panel.Children.Add(Row("Sitekey:", Txt(nameof(BlockRecaptcha.SiteKey))));
    }
}
