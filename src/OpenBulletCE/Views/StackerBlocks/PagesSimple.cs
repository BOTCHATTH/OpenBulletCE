using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RuriLib;
using RuriLib.Blocks;
using RuriLib.Functions.Requests;

namespace OpenBulletCE.Views.StackerBlocks;

public class PageBlockTCP : BlockPage
{
    public PageBlockTCP(BlockTCP b) : base(b)
    {
        var capRow = new DockPanel();
        capRow.Children.Add(Check("Is Capture", nameof(BlockTCP.IsCapture)));
        capRow.Children.Add(new TextBlock { Text = "  Variable Name:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(10, 0, 5, 0) });
        capRow.Children.Add(Txt(nameof(BlockTCP.VariableName)));
        Panel.Children.Add(capRow);

        var cmdRow = new DockPanel { Margin = new Avalonia.Thickness(0, 4, 0, 0) };
        cmdRow.Children.Add(new TextBlock { Text = "Command:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        cmdRow.Children.Add(EnumCombo<TCPCommand>(b.TCPCommand, v => b.TCPCommand = v));
        Panel.Children.Add(cmdRow);

        var hostRow = new DockPanel();
        var portRow = Row("Port:", Txt(nameof(BlockTCP.Port)), 40);
        DockPanel.SetDock(portRow, Dock.Right);
        hostRow.Children.Add(portRow);
        hostRow.Children.Add(Row("Host:", Txt(nameof(BlockTCP.Host)), 40));
        Panel.Children.Add(hostRow);

        var opts = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        opts.Children.Add(Check("SSL", nameof(BlockTCP.UseSSL)));
        opts.Children.Add(Check("WebSocket", nameof(BlockTCP.WebSocket)));
        opts.Children.Add(Check("Wait for hello", nameof(BlockTCP.WaitForHello)));
        Panel.Children.Add(opts);

        Panel.Children.Add(Row("Message:", Txt(nameof(BlockTCP.Message), "Remember to end your message with \\r\\n to let the server know it's over!")));
    }
}

public class PageBlockBypassCF : BlockPage
{
    public PageBlockBypassCF(BlockBypassCF b) : base(b)
    {
        Panel.Children.Add(Row("URL:", Txt(nameof(BlockBypassCF.Url))));
        Panel.Children.Add(Row("User Agent:", Txt(nameof(BlockBypassCF.UserAgent))));
        Panel.Children.Add(Row("Security Protocol:", EnumCombo<SecurityProtocol>(b.SecurityProtocol, v => b.SecurityProtocol = v), 100));
        Panel.Children.Add(Check("Print Response Info", nameof(BlockBypassCF.PrintResponseInfo)));
        Panel.Children.Add(Check("Auto Redirect (might not work on some sites)", nameof(BlockBypassCF.AutoRedirect)));
    }
}

public class PageBlockLSCode : BlockPage
{
    public PageBlockLSCode(BlockLSCode b) : base(b)
    {
        Panel.Children.Add(new TextBlock { Text = "LoliScript Code (read-only, switch view to edit):" });
        var tb = new TextBox
        {
            Text = b.Script,
            IsReadOnly = true,
            AcceptsReturn = true,
            FontFamily = new FontFamily("Consolas"),
            MinHeight = 300,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Panel.Children.Add(tb);
    }
}

public class PageBlockCookieContainer : BlockPage
{
    public PageBlockCookieContainer(BlockCookieContainer b) : base(b)
    {
        Panel.Children.Add(Row("Variable Name:", Txt(nameof(BlockCookieContainer.VariableName))));
        Panel.Children.Add(Row("Input string:", Txt(nameof(BlockCookieContainer.InputString))));
        Panel.Children.Add(Row("Domain:", Txt(nameof(BlockCookieContainer.Domain))));
        Panel.Children.Add(Check("Save netscape?", nameof(BlockCookieContainer.SaveNetscape)));
    }
}

public class PageSBlockBrowserAction : BlockPage
{
    public PageSBlockBrowserAction(SBlockBrowserAction b) : base(b)
    {
        Panel.Children.Add(Row("Action:", EnumCombo<BrowserAction>(b.Action, v => b.Action = v), 50));
        Panel.Children.Add(Row("Input:", Txt(nameof(SBlockBrowserAction.Input))));
    }
}

public class PageSBlockElementAction : BlockPage
{
    public PageSBlockElementAction(SBlockElementAction b) : base(b)
    {
        var findRow = new DockPanel();
        findRow.Children.Add(new TextBlock { Text = "Find Element By:", VerticalAlignment = VerticalAlignment.Center });
        findRow.Children.Add(EnumCombo<ElementLocator>(b.Locator, v => b.Locator = v));
        findRow.Children.Add(new TextBlock { Text = "=", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(5, 0) });
        findRow.Children.Add(Txt(nameof(SBlockElementAction.ElementString)));
        Panel.Children.Add(findRow);

        var idxRow = new DockPanel();
        idxRow.Children.Add(new TextBlock { Text = "With Index =", VerticalAlignment = VerticalAlignment.Center });
        idxRow.Children.Add(Num(nameof(SBlockElementAction.ElementIndex), 0, 10000));
        idxRow.Children.Add(Check("Recursive (all indexes)", nameof(SBlockElementAction.Recursive)));
        Panel.Children.Add(idxRow);

        Panel.Children.Add(Row("Action:", EnumCombo<ElementAction>(b.Action, v => b.Action = v), 50));
        Panel.Children.Add(Row("Input:", Txt(nameof(SBlockElementAction.Input))));

        var capRow = new DockPanel();
        capRow.Children.Add(Check("Is Capture", nameof(SBlockElementAction.IsCapture)));
        capRow.Children.Add(new TextBlock { Text = "  Output Variable:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(10, 0, 5, 0) });
        capRow.Children.Add(Txt(nameof(SBlockElementAction.OutputVariable)));
        Panel.Children.Add(capRow);
    }
}

public class PageSBlockExecuteJS : BlockPage
{
    public PageSBlockExecuteJS(SBlockExecuteJS b) : base(b)
    {
        Panel.Children.Add(new TextBlock { Text = "JavaScript:" });
        var js = new TextBox { AcceptsReturn = true, MinHeight = 200, FontFamily = new FontFamily("Consolas"), TextWrapping = TextWrapping.Wrap };
        js.Text = b.JavascriptCode;
        js.Bind(TextBox.TextProperty, new Avalonia.Data.Binding(nameof(SBlockExecuteJS.JavascriptCode)) { Mode = Avalonia.Data.BindingMode.TwoWay, Source = b });
        Panel.Children.Add(js);

        var capRow = new DockPanel();
        capRow.Children.Add(Check("Is Capture", nameof(SBlockExecuteJS.IsCapture)));
        capRow.Children.Add(new TextBlock { Text = "  Output Variable:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(10, 0, 5, 0) });
        capRow.Children.Add(Txt(nameof(SBlockExecuteJS.OutputVariable)));
        Panel.Children.Add(capRow);
    }
}

public class PageSBlockNavigate : BlockPage
{
    public PageSBlockNavigate(SBlockNavigate b) : base(b)
    {
        Panel.Children.Add(Row("URL:", Txt(nameof(SBlockNavigate.Url))));
        Panel.Children.Add(Row("Timeout (seconds):", Num(nameof(SBlockNavigate.Timeout), 0, 600), 120));
        Panel.Children.Add(Check("Ban on Timeout", nameof(SBlockNavigate.BanOnTimeout)));
    }
}

public class PageBlockImageCaptcha : BlockPage
{
    public PageBlockImageCaptcha(BlockImageCaptcha b) : base(b)
    {
        Panel.Children.Add(Row("Variable Name:", Txt(nameof(BlockImageCaptcha.VariableName))));
        Panel.Children.Add(Row("Captcha URL:", Txt(nameof(BlockImageCaptcha.Url))));
        Panel.Children.Add(Row("User Agent:", Txt(nameof(BlockImageCaptcha.UserAgent))));
        Panel.Children.Add(Check("Base 64 Captcha in Url box", nameof(BlockImageCaptcha.Base64)));
        Panel.Children.Add(Check("Use Previously Saved Screenshot", nameof(BlockImageCaptcha.SendScreenshot)));
    }
}

public class PageBlockReportCaptcha : BlockPage
{
    public PageBlockReportCaptcha(BlockReportCaptcha b) : base(b)
    {
        Panel.Children.Add(Row("Captcha Type:", EnumCombo<CaptchaSharp.Enums.CaptchaType>(b.Type, v => b.Type = v), 90));
        Panel.Children.Add(Row("Captcha ID:", Txt(nameof(BlockReportCaptcha.CaptchaId))));
    }
}

public class PageBlockScript : BlockPage
{
    private static readonly string[] Interpreters = { "Jint", "NodeJS", "IronPython" };
    private static readonly string[] OutputTypes = { "String", "Int", "Float", "Bool", "ListOfStrings", "DictionaryOfStrings", "ByteArray" };

    public PageBlockScript(BlockScript b) : base(b)
    {
        Panel.Children.Add(new TextBlock { Text = "Settings:", FontWeight = FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 2, 0, 4) });
        Panel.Children.Add(Row("Input variables:", Txt(nameof(BlockScript.InputVariablesText), "Comma-separated variable names, e.g. x,y")));
        Panel.Children.Add(Row("Interpreter:", StrCombo(Interpreters, b.Interpreter, v => b.Interpreter = v), 100));

        var outputsPanel = new StackPanel { Spacing = 2 };

        var outHeader = new DockPanel { Margin = new Avalonia.Thickness(0, 6, 0, 2) };
        outHeader.Children.Add(new TextBlock { Text = "Output variables:", VerticalAlignment = VerticalAlignment.Center });
        var addBtn = new Button
        {
            Content = "+ Add",
            Padding = new Avalonia.Thickness(10, 2),
            Margin = new Avalonia.Thickness(10, 0, 0, 0),
            Background = Brush("BrushAccent"),
            Foreground = Brushes.White
        };
        DockPanel.SetDock(addBtn, Dock.Right);
        outHeader.Children.Add(addBtn);
        Panel.Children.Add(outHeader);
        Panel.Children.Add(outputsPanel);

        void RebuildOutputs()
        {
            outputsPanel.Children.Clear();
            foreach (var o in b.Outputs)
            {
                var row = new DockPanel { Margin = new Avalonia.Thickness(0, 1) };

                var typeCombo = StrCombo(OutputTypes, o.Type, v => o.Type = v);
                typeCombo.Width = 150;
                DockPanel.SetDock(typeCombo, Dock.Left);
                row.Children.Add(typeCombo);

                var delBtn = new Button { Content = "✕", Padding = new Avalonia.Thickness(8, 2), Margin = new Avalonia.Thickness(4, 0, 0, 0) };
                DockPanel.SetDock(delBtn, Dock.Right);
                row.Children.Add(delBtn);

                var nameBox = new TextBox { Text = o.Name, MinHeight = 24, Margin = new Avalonia.Thickness(4, 0, 0, 0), Padding = new Avalonia.Thickness(6, 2), Watermark = "name (@ for capture)" };
                var captured = o;
                nameBox.PropertyChanged += (s, e) =>
                {
                    if (e.Property == TextBox.TextProperty) captured.Name = nameBox.Text ?? "";
                };
                row.Children.Add(nameBox);

                delBtn.Click += (s, e) => { b.Outputs.Remove(captured); RebuildOutputs(); };
                outputsPanel.Children.Add(row);
            }
        }

        addBtn.Click += (s, e) => { b.Outputs.Add(new ScriptOutput()); RebuildOutputs(); };
        RebuildOutputs();

        Panel.Children.Add(new TextBlock { Text = "Script:", Margin = new Avalonia.Thickness(0, 8, 0, 2) });
        Panel.Children.Add(MultiBox(b.Script, v => b.Script = v, 200));
    }
}
