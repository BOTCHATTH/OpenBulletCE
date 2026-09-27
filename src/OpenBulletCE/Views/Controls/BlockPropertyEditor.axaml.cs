using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using RuriLib;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace OpenBulletCE.Views.Controls;

/// <summary>
/// Dynamically generates property editors for any BlockBase subclass.
/// Reads public writable properties and creates appropriate controls:
/// - string -> TextBox
/// - bool -> CheckBox
/// - enum -> ComboBox
/// - int/long/double -> NumericUpDown
/// - everything else -> read-only label
/// </summary>
public partial class BlockPropertyEditor : UserControl
{
    private static readonly string[] SkipProperties = new[]
    {
        "Block", "Color", "Foreground", "BorderColor", "Height", "FontSize",
        "Id", "IsSelected", "ControlsEnabled"
    };

    private BlockBase _block;

    public BlockPropertyEditor()
    {
        InitializeComponent();
    }

    public BlockPropertyEditor(BlockBase block) : this()
    {
        SetBlock(block);
    }

    public void SetBlock(BlockBase block)
    {
        _block = block;
        PropertiesPanel.DataContext = block;
        BuildEditor();
    }

    private void BuildEditor()
    {
        PropertiesPanel.Children.Clear();
        if (_block == null) return;

        var props = _block.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.CanRead && !SkipProperties.Contains(p.Name))
            .OrderBy(p => p.Name)
            .ToList();

        foreach (var prop in props)
        {
            var editor = CreateEditor(prop);
            if (editor != null)
            {
                var row = new DockPanel { Margin = new Avalonia.Thickness(0, 2) };
                var label = new TextBlock
                {
                    Text = FormatName(prop.Name) + ":",
                    Foreground = GetBrush("BrushTextSecondary"),
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 120,
                    Margin = new Avalonia.Thickness(0, 0, 8, 0)
                };
                DockPanel.SetDock(label, Dock.Left);
                row.Children.Add(label);
                row.Children.Add(editor);
                PropertiesPanel.Children.Add(row);
            }
        }
    }

    private Control CreateEditor(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        var binding = new Binding(prop.Name) { Mode = BindingMode.TwoWay };

        // String
        if (type == typeof(string))
        {
            var tb = new TextBox();
            tb.Bind(TextBox.TextProperty, binding);
            return tb;
        }

        // Bool
        if (type == typeof(bool))
        {
            var cb = new CheckBox();
            cb.Bind(CheckBox.IsCheckedProperty, binding);
            cb.VerticalAlignment = VerticalAlignment.Center;
            return cb;
        }

        // Enum
        if (type.IsEnum)
        {
            var cb = new ComboBox();
            foreach (var val in Enum.GetValues(type))
                cb.Items.Add(val.ToString());
            var current = prop.GetValue(_block);
            if (current != null)
                cb.SelectedItem = current.ToString();
            cb.SelectionChanged += (s, e) =>
            {
                if (cb.SelectedItem != null)
                    prop.SetValue(_block, Enum.Parse(type, cb.SelectedItem.ToString()));
            };
            return cb;
        }

        // Numeric types
        if (type == typeof(int) || type == typeof(long) || type == typeof(double) ||
            type == typeof(float) || type == typeof(decimal))
        {
            var nud = new NumericUpDown();
            var val = prop.GetValue(_block);
            if (val != null)
                nud.Value = Convert.ToDecimal(val);
            nud.ValueChanged += (s, e) =>
            {
                try { prop.SetValue(_block, Convert.ChangeType(nud.Value ?? 0, type)); } catch { }
            };
            return nud;
        }

        // Lists/collections — show count
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var items = prop.GetValue(_block) as IEnumerable;
            var count = items?.Cast<object>().Count() ?? 0;
            return new TextBlock
            {
                Text = $"({count} items)",
                Foreground = GetBrush("BrushTextMuted"),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        // Read-only fallback
        var display = new TextBlock
        {
            Text = prop.GetValue(_block)?.ToString() ?? "(null)",
            Foreground = GetBrush("BrushTextMuted"),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        return display;
    }

    private static string FormatName(string name)
    {
        // Insert spaces before capital letters: "ReadResponseSource" -> "Read Response Source"
        var result = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
                result.Append(' ');
            result.Append(c);
        }
        return result.ToString();
    }

    private static IBrush GetBrush(string key)
    {
        if (Avalonia.Application.Current?.TryGetResource(key, Avalonia.Styling.ThemeVariant.Default, out var res) == true && res is IBrush brush)
            return brush;
        return Brushes.Gray;
    }
}
