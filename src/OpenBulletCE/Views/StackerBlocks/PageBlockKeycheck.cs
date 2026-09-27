using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using OpenBulletCE.ViewModels;
using RuriLib;
using RuriLib.Functions.Conditions;
using RuriLib.Models;
using System;

namespace OpenBulletCE.Views.StackerBlocks;

public class PageBlockKeycheck : BlockPage
{
    private readonly BlockKeycheck _b;
    private readonly StackPanel _kcPanel = new() { Spacing = 6 };
    private readonly Random _rand = new(3);

    // Variables offered by the [T] picker (OB2 parity)
    private static readonly string[] KnownVars =
    {
        "data.SOURCE", "data.RAWSOURCE", "data.ADDRESS", "data.RESPONSECODE", "data.STATUS",
        "data.ERROR", "data.COOKIES[\"name\"]", "data.HEADERS[\"name\"]", "data.Line.Data",
        "input.USERNAME", "input.PASSWORD", "input.USER", "input.PASS", "input.URL"
    };

    public PageBlockKeycheck(BlockKeycheck block) : base(block)
    {
        _b = block;

        Panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 14,
            Children =
            {
                Check("Ban If No Match", nameof(BlockKeycheck.BanOnToCheck),
                      "If no keychain matches the data, the bot status is set to BAN"),
                Check("Insta Ban 4xx", nameof(BlockKeycheck.BanOn4XX))
            }
        });

        var kcHeader = new DockPanel { Margin = new Avalonia.Thickness(0, 6, 0, 0) };
        kcHeader.Children.Add(new TextBlock { Text = "Keychains:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold });
        var addBtn = new Button { Content = "+ Add", Padding = new Avalonia.Thickness(10, 2), Margin = new Avalonia.Thickness(8, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Classes.Add("Success");
        addBtn.Click += (s, e) => AddKeychain();
        DockPanel.SetDock(addBtn, Dock.Left);
        kcHeader.Children.Add(addBtn);
        Panel.Children.Add(kcHeader);

        var scroll = new ScrollViewer { Content = _kcPanel, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        Panel.Children.Add(scroll);

        foreach (var kc in _b.KeyChains)
            _kcPanel.Children.Add(BuildKeychain(new KeychainViewModel(kc, _rand.Next())));
    }

    private void AddKeychain()
    {
        var kc = new KeyChain();
        _b.KeyChains.Add(kc);
        _kcPanel.Children.Add(BuildKeychain(new KeychainViewModel(kc, _rand.Next())));
    }

    private Control BuildKeychain(KeychainViewModel kvm)
    {
        // Card: dark surface, thin left accent strip in the keychain's status color (OB2 look)
        var inner = new StackPanel { Spacing = 6 };
        var card = new Grid { ColumnDefinitions = new ColumnDefinitions("4,*") };
        var accent = new Border { Width = 4, CornerRadius = new Avalonia.CornerRadius(2, 0, 0, 2) };
        accent.Bind(Border.BackgroundProperty, new Binding(nameof(KeychainViewModel.KeychainColor)) { Source = kvm });
        Grid.SetColumn(accent, 0);
        card.Children.Add(accent);
        var body = new Border
        {
            Padding = new Avalonia.Thickness(8, 6),
            Background = Brush("BrushSurfaceAlt"),
            Child = inner
        };
        Grid.SetColumn(body, 1);
        card.Children.Add(body);

        var border = new Border
        {
            Child = card,
            CornerRadius = new Avalonia.CornerRadius(4),
            BorderBrush = Brush("BrushBorder"),
            BorderThickness = new Avalonia.Thickness(1),
            Margin = new Avalonia.Thickness(0, 2)
        };

        // Row 0: Result status + Mode + custom type (left) / up, down, remove (right)
        var top = new DockPanel();

        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        left.Children.Add(new TextBlock { Text = "Result status:", VerticalAlignment = VerticalAlignment.Center });
        var typeCombo = new ComboBox { MinHeight = 26, MinWidth = 110 };
        foreach (var t in Enum.GetValues<KeyChain.KeychainType>()) typeCombo.Items.Add(t);
        typeCombo.SelectedItem = kvm.Type;
        typeCombo.SelectionChanged += (s, e) => { if (typeCombo.SelectedItem is KeyChain.KeychainType t) kvm.Type = t; };
        left.Children.Add(typeCombo);
        left.Children.Add(new TextBlock { Text = "Mode:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(8, 0, 0, 0) });
        var modeCombo = new ComboBox { MinHeight = 26, MinWidth = 72 };
        foreach (var m in Enum.GetValues<KeyChain.KeychainMode>()) modeCombo.Items.Add(m);
        modeCombo.SelectedItem = kvm.Mode;
        modeCombo.SelectionChanged += (s, e) => { if (modeCombo.SelectedItem is KeyChain.KeychainMode m) kvm.Mode = m; };
        left.Children.Add(modeCombo);
        var customCombo = new ComboBox { MinHeight = 26, MinWidth = 100, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        foreach (var n in OB.Settings.Environment.GetCustomKeychainNames()) customCombo.Items.Add(n);
        customCombo.SelectedItem = kvm.CustomType;
        customCombo.Bind(ComboBox.IsVisibleProperty, new Binding(nameof(KeychainViewModel.CustomVisibility)) { Source = kvm });
        customCombo.SelectionChanged += (s, e) => { if (customCombo.SelectedItem is string ct) kvm.CustomType = ct; };
        left.Children.Add(customCombo);
        DockPanel.SetDock(left, Dock.Left);
        top.Children.Add(left);

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        var up = IconBtn("M7,15L12,10L17,15H7Z", "Move keychain up");
        up.Click += (s, e) => MoveKeychain(border, kvm, -1);
        var down = IconBtn("M7,10L12,15L17,10H7Z", "Move keychain down");
        down.Click += (s, e) => MoveKeychain(border, kvm, 1);
        var rmKc = IconBtn("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z", "Remove keychain");
        rmKc.Click += (s, e) =>
        {
            _b.KeyChains.Remove(kvm.Keychain);
            _kcPanel.Children.Remove(border);
        };
        right.Children.Add(up);
        right.Children.Add(down);
        right.Children.Add(rmKc);
        DockPanel.SetDock(right, Dock.Right);
        top.Children.Add(right);
        inner.Children.Add(top);

        // Row 1: typed add buttons (green, OB2 style)
        var adds = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        foreach (var (label, comparer) in new (string, Comparer)[]
        {
            ("+ String", Comparer.Contains),
            ("+ Int", Comparer.EqualTo),
            ("+ Float", Comparer.EqualTo),
            ("+ List", Comparer.Contains),
        })
        {
            var b = new Button { Content = label, Padding = new Avalonia.Thickness(8, 2), MinHeight = 24 };
            b.Classes.Add("Success");
            var cmp = comparer;
            b.Click += (s, e) => { kvm.AddKey(); kvm.KeyList[^1].Comparer = cmp; };
            adds.Children.Add(b);
        }
        inner.Children.Add(adds);

        // Row 2: keys
        var keys = new ItemsControl
        {
            ItemsSource = kvm.KeyList,
            ItemTemplate = new FuncDataTemplate<KeyViewModel>((k, ns) => BuildKeyRow(kvm, k))
        };
        var keysScroll = new ScrollViewer { Content = keys, MaxHeight = 200, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        inner.Children.Add(keysScroll);

        return border;
    }

    private void MoveKeychain(Control card, KeychainViewModel kvm, int delta)
    {
        var i = _b.KeyChains.IndexOf(kvm.Keychain);
        var j = i + delta;
        if (i < 0 || j < 0 || j >= _b.KeyChains.Count) return;
        var kc = _b.KeyChains[i];
        _b.KeyChains.RemoveAt(i);
        _b.KeyChains.Insert(j, kc);
        _kcPanel.Children.Move(_kcPanel.Children.IndexOf(card), j);
    }

    private Control BuildKeyRow(KeychainViewModel kvm, KeyViewModel k)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Avalonia.Thickness(0, 2) };

        var left = new TextBox { MinWidth = 130, MinHeight = 26, Padding = new Avalonia.Thickness(6, 2) };
        left.Bind(TextBox.TextProperty, new Binding(nameof(KeyViewModel.LeftTerm)) { Mode = BindingMode.TwoWay, Source = k });
        row.Children.Add(left);
        row.Children.Add(VarBtn(left));

        var comp = new ComboBox { MinHeight = 26, MinWidth = 120 };
        foreach (var c in Enum.GetValues<Comparer>()) comp.Items.Add(c);
        comp.SelectedItem = k.Comparer;
        comp.SelectionChanged += (s, e) => { if (comp.SelectedItem is Comparer c) k.Comparer = c; };
        row.Children.Add(comp);

        var right = new TextBox { MinWidth = 150, MinHeight = 26, Padding = new Avalonia.Thickness(6, 2) };
        right.Bind(TextBox.TextProperty, new Binding(nameof(KeyViewModel.RightTerm)) { Mode = BindingMode.TwoWay, Source = k });
        row.Children.Add(right);
        row.Children.Add(VarBtn(right));

        var rm = IconBtn("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z", "Remove key");
        rm.Click += (s, e) => kvm.RemoveKeyById(k.Id.KeyId);
        row.Children.Add(rm);

        return row;
    }

    /// <summary>Small [T] button — opens a flyout with known variables to insert.</summary>
    private Button VarBtn(TextBox target)
    {
        var b = new Button { Content = "T", Width = 24, MinHeight = 24, Padding = new Avalonia.Thickness(0), FontWeight = FontWeight.Bold };
        var fly = new MenuFlyout();
        foreach (var v in KnownVars)
        {
            var item = new MenuItem { Header = v };
            var val = v;
            item.Click += (s, e) =>
            {
                var caret = target.CaretIndex;
                var text = target.Text ?? "";
                target.Text = text.Insert(caret >= 0 ? caret : text.Length, val);
                target.CaretIndex = (caret >= 0 ? caret : text.Length) + val.Length;
                target.Focus();
            };
            fly.Items.Add(item);
        }
        b.Flyout = fly;
        return b;
    }

    private Button IconBtn(string iconPath, string tip)
    {
        var b = new Button
        {
            Content = new PathIcon { Data = Geometry.Parse(iconPath), Width = 12, Height = 12 },
            Width = 26,
            MinHeight = 24,
            Padding = new Avalonia.Thickness(0)
        };
        ToolTip.SetTip(b, tip);
        return b;
    }
}
