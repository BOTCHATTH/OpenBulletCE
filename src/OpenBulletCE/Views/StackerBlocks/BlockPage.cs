using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using RuriLib;
using System;

namespace OpenBulletCE.Views.StackerBlocks;

/// <summary>
/// Base class for per-block editor pages — mirrors the original WPF StackerBlocks pages.
/// DataContext is the BlockBase itself, so {Binding Prop} writes straight to the block.
/// </summary>
public abstract class BlockPage : UserControl
{
    protected readonly BlockBase Block;
    protected readonly StackPanel Panel = new() { Spacing = 2 };

    protected BlockPage(BlockBase block)
    {
        Block = block;
        DataContext = block;
        Content = new ScrollViewer { Content = Panel };
    }

    public static IBrush Brush(string key)
    {
        if (Avalonia.Application.Current?.TryGetResource(key, Avalonia.Styling.ThemeVariant.Default, out var res) == true && res is IBrush b)
            return b;
        return Brushes.Gray;
    }

    /// <summary>DockPanel row: label left, control fills.</summary>
    protected DockPanel Row(string label, Control c, double labelWidth = 110)
    {
        var row = new DockPanel { Margin = new Avalonia.Thickness(0, 2) };
        var lbl = new TextBlock
        {
            Text = label,
            Foreground = Brush("BrushTextPrimary"),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = labelWidth
        };
        DockPanel.SetDock(lbl, Dock.Left);
        row.Children.Add(lbl);
        row.Children.Add(c);
        return row;
    }

    /// <summary>TextBox two-way bound to a string prop on the block.</summary>
    protected TextBox Txt(string prop, string tooltip = null)
    {
        var tb = new TextBox { MinHeight = 24, Padding = new Avalonia.Thickness(6, 2) };
        tb.Bind(TextBox.TextProperty, new Binding(prop) { Mode = BindingMode.TwoWay });
        if (tooltip != null) ToolTip.SetTip(tb, tooltip);
        return tb;
    }

    /// <summary>CheckBox two-way bound to a bool prop.</summary>
    protected CheckBox Check(string label, string prop, string tooltip = null)
    {
        var cb = new CheckBox
        {
            Content = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 2)
        };
        cb.Bind(CheckBox.IsCheckedProperty, new Binding(prop) { Mode = BindingMode.TwoWay });
        if (tooltip != null) ToolTip.SetTip(cb, tooltip);
        return cb;
    }

    /// <summary>ComboBox populated from an enum; writes back the enum value.</summary>
    protected ComboBox EnumCombo<T>(T current, Action<T> onChange) where T : struct, Enum
    {
        var cb = new ComboBox { MinHeight = 24 };
        foreach (var name in Enum.GetNames(typeof(T)))
            cb.Items.Add(name);
        cb.SelectedIndex = System.Convert.ToInt32(current);
        cb.SelectionChanged += (s, e) =>
        {
            if (cb.SelectedIndex >= 0)
                onChange((T)Enum.ToObject(typeof(T), cb.SelectedIndex));
        };
        return cb;
    }

    /// <summary>ComboBox with plain string items.</summary>
    protected ComboBox StrCombo(string[] items, string current, Action<string> onChange)
    {
        var cb = new ComboBox { MinHeight = 24 };
        foreach (var i in items) cb.Items.Add(i);
        cb.SelectedItem = current;
        cb.SelectionChanged += (s, e) =>
        {
            if (cb.SelectedItem != null)
                onChange(cb.SelectedItem.ToString());
        };
        return cb;
    }

    /// <summary>NumericUpDown bound to an int prop.</summary>
    protected NumericUpDown Num(string prop, double min = 0, double max = 100000, double width = 80)
    {
        var n = new NumericUpDown { Minimum = (decimal)min, Maximum = (decimal)max, Width = width, MinHeight = 24, Padding = new Avalonia.Thickness(6, 2), HorizontalAlignment = HorizontalAlignment.Left };
        n.Bind(NumericUpDown.ValueProperty, new Binding(prop) { Mode = BindingMode.TwoWay });
        return n;
    }

    /// <summary>Multi-line TextBox that serializes to/from string props (or via callbacks).</summary>
    protected TextBox MultiBox(string initial, Action<string> save, double height = 80, string tooltip = null)
    {
        var tb = new TextBox
        {
            Text = initial,
            AcceptsReturn = true,
            Height = height,
            FontFamily = new FontFamily("Consolas"),
            MinHeight = 40,
            Padding = new Avalonia.Thickness(6, 4),
            TextWrapping = TextWrapping.NoWrap
        };
        tb.LostFocus += (s, e) => save?.Invoke(tb.Text ?? "");
        if (tooltip != null) ToolTip.SetTip(tb, tooltip);
        return tb;
    }

    protected TextBlock Warn(string text) => new()
    {
        Text = text,
        Foreground = Brush("BrushCustom"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Avalonia.Thickness(0, 8, 0, 0)
    };

    protected TextBlock Note(string text) => new()
    {
        Text = text,
        Foreground = Brush("BrushTextPrimary"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Avalonia.Thickness(0, 8, 0, 0)
    };
}

/// <summary>Resolves the editor page for a block type.</summary>
public static class BlockViewFactory
{
    /// <summary>OB2-style "Name (TypeName)" + short description shown above the block editor.</summary>
    private static (string Name, string Desc) InfoFor(BlockBase b) => b switch
    {
        BlockRequest => ("Http Request", "Performs an http request and reads the response"),
        BlockKeycheck => ("Keycheck", "Checks whether the bot status should be hit, fail, ban, retry or custom"),
        BlockParse => ("Parse", "Parses values from the response source"),
        BlockFunction => ("Function", "Performs functions on variables"),
        BlockUtility => ("Utility", "Utility functions for lists, dictionaries, files and variables"),
        BlockSolveCaptcha => ("Solve Captcha", "Solves a captcha challenge through a solver service"),
        BlockReportCaptcha => ("Report Captcha", "Reports the last captcha as correctly or incorrectly solved"),
        BlockImageCaptcha => ("Image Captcha", "Solves an image captcha challenge"),
        BlockRecaptcha => ("Recaptcha", "Solves a reCAPTCHA challenge"),
        BlockBypassCF => ("Bypass CF", "Bypasses Cloudflare protection"),
        BlockTCP => ("TCP", "Connects to a host over TCP and exchanges data"),
        BlockLSCode => ("LoliScript", "Executes raw LoliScript code"),
        BlockScript => ("Script", "This block can invoke a script in a different language, pass some variables and return some results."),
        RuriLib.Blocks.BlockCookieContainer => ("Cookie", "Sets a cookie in the cookie container"),
        SBlockNavigate => ("Navigate", "Navigates the selenium browser to a page"),
        SBlockBrowserAction => ("Browser Action", "Performs an action on the selenium browser"),
        SBlockElementAction => ("Element Action", "Performs an action on a page element"),
        SBlockExecuteJS => ("Execute JS", "Executes javascript in the selenium browser"),
        _ => (b.GetType().Name.Replace("Block", ""), "")
    };

    public static Control Create(BlockBase block)
    {
        Control page = block switch
        {
            BlockRequest b => new PageBlockRequest(b),
            BlockParse b => new PageBlockParse(b),
            BlockFunction b => new PageBlockFunction(b),
            BlockUtility b => new PageBlockUtility(b),
            BlockKeycheck b => new PageBlockKeycheck(b),
            BlockTCP b => new PageBlockTCP(b),
            BlockBypassCF b => new PageBlockBypassCF(b),
            BlockLSCode b => new PageBlockLSCode(b),
            BlockScript b => new PageBlockScript(b),
            RuriLib.Blocks.BlockCookieContainer b => new PageBlockCookieContainer(b),
            BlockImageCaptcha b => new PageBlockImageCaptcha(b),
            BlockRecaptcha b => new PageBlockRecaptcha(b),
            BlockSolveCaptcha b => new PageBlockSolveCaptcha(b),
            BlockReportCaptcha b => new PageBlockReportCaptcha(b),
            SBlockNavigate b => new PageSBlockNavigate(b),
            SBlockBrowserAction b => new PageSBlockBrowserAction(b),
            SBlockElementAction b => new PageSBlockElementAction(b),
            SBlockExecuteJS b => new PageSBlockExecuteJS(b),
            _ => null
        };
        page ??= new Controls.BlockPropertyEditor(block);

        var (name, desc) = InfoFor(block);
        var typeName = block.GetType().Name;
        var header = new StackPanel { Margin = new Avalonia.Thickness(0, 0, 0, 8) };
        header.Children.Add(new TextBlock
        {
            Text = $"{name} ({typeName})",
            FontWeight = FontWeight.Bold,
            Foreground = BlockPage.Brush("BrushAccent")
        });
        if (desc.Length > 0)
            header.Children.Add(new TextBlock
            {
                Text = desc,
                Foreground = BlockPage.Brush("BrushTextSecondary"),
                TextWrapping = TextWrapping.Wrap
            });
        header.Children.Add(new Border
        {
            Height = 1,
            Background = BlockPage.Brush("BrushBorder"),
            Margin = new Avalonia.Thickness(0, 6, 0, 0)
        });
        return new StackPanel { Children = { header, page } };
    }
}
