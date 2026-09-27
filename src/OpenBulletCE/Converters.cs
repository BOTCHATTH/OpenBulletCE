using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace OpenBulletCE;

/// <summary>Converts RuriLib.Models.Color to an Avalonia brush for colored log output.</summary>
public class LogColorConverter : IValueConverter
{
    public static readonly LogColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RuriLib.Models.Color c)
            return new Avalonia.Media.Immutable.ImmutableSolidColorBrush(
                Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B));
        return Avalonia.Media.Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns "Accent" class string when value equals the parameter, empty otherwise.</summary>
public class TabActiveConverter : IValueConverter
{
    public static readonly TabActiveConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value?.ToString() == parameter?.ToString())
            return "Accent";
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns "Accent" class string when the StackerView matches the parameter.</summary>
public class StackerViewConverter : IValueConverter
{
    public static readonly StackerViewConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value?.ToString() == parameter?.ToString())
            return "Accent";
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns true when value equals the parameter (for input-type visibility).</summary>
public class InputTypeConverter : IValueConverter
{
    public static readonly InputTypeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts object to int for NumericUpDown binding.</summary>
public class IntConverter : IValueConverter
{
    public static readonly IntConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return 0;
        try { return System.Convert.ToInt32(value); } catch { return 0; }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}

/// <summary>Returns true when value equals the parameter (string comparison).</summary>
public class EqualConverter : IValueConverter
{
    public static readonly EqualConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns true when value is not null.</summary>
public class NotNullConverter : IValueConverter
{
    public static readonly NotNullConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts a base64 image string to an Avalonia Bitmap.</summary>
public class Base64ToBitmapConverter : IValueConverter
{
    public static readonly Base64ToBitmapConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                var bytes = System.Convert.FromBase64String(s);
                return new Avalonia.Media.Imaging.Bitmap(new System.IO.MemoryStream(bytes));
            }
            catch { }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts a base64 image string to a faint full-bleed ImageBrush (page background).</summary>
public class Base64ToImageBrushConverter : IValueConverter
{
    public static readonly Base64ToImageBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var bmp = Base64ToBitmapConverter.Instance.Convert(value, targetType, parameter, culture) as Avalonia.Media.Imaging.Bitmap;
        if (bmp == null) return null;
        return new Avalonia.Media.ImageBrush(bmp)
        {
            Stretch = Avalonia.Media.Stretch.UniformToFill,
            Opacity = 0.14
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>WorkerStatus -> "START"/"STOP" button text.</summary>
public class StartStopTextConverter : IValueConverter
{
    public static readonly StartStopTextConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == "Running" ? "STOP" : "START";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Loads a Bitmap from a file path; null when missing/invalid.</summary>
public class PathToBitmapConverter : IValueConverter
{
    public static readonly PathToBitmapConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s) && System.IO.File.Exists(s))
                return new Avalonia.Media.Imaging.Bitmap(s);
        }
        catch { }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts a hex color string (#AARRGGBB or #RRGGBB) to a solid brush.</summary>
public class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Avalonia.Media.Color.Parse(s));
        }
        catch { }
        return Avalonia.Media.Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>int &lt;-&gt; decimal? bridge for NumericUpDown: tolerates empty (null) input.</summary>
public class IntDecimalConverter : IValueConverter
{
    public static readonly IntDecimalConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return (decimal?)null;
        try { return System.Convert.ToDecimal(value); } catch { return (decimal?)null; }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return 0;
        try { return (int)System.Convert.ToDecimal(value); } catch { return 0; }
    }
}

/// <summary>Converts object to bool for CheckBox binding.</summary>
public class BoolConverter : IValueConverter
{
    public static readonly BoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return false;
        try { return System.Convert.ToBoolean(value); } catch { return false; }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}

/// <summary>Colors the hit Type column text: HIT green, CUSTOM orange, TOCHECK light blue, etc.</summary>
public class HitTypeColorConverter : IValueConverter
{
    public static readonly HitTypeColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var t = value?.ToString()?.ToUpperInvariant() ?? "";
        var color = t switch
        {
            "HIT" or "SUCCESS" => "#ADFF2F",
            "CUSTOM" => "#FF8C00",
            "TOCHECK" or "NONE" => "#87CEFA",
            "FAIL" or "ERROR" => "#FF6347",
            "BAN" or "RETRY" => "#FFD700",
            _ => "#FFFFFF"
        };
        return new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Avalonia.Media.Color.Parse(color));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
