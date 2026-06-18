using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CassaEventiAI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => v is Visibility.Visible;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => v is Visibility.Collapsed;
}

public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is null || (v is string s && string.IsNullOrEmpty(s))
            ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class EuroConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is decimal d ? $"€ {d:F2}" : "€ 0,00";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is bool b && !b;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => v is bool b && !b;
}

public class BoolToFirstRunLabelConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? "Prima configurazione — crea evento e account amministratore" : "Crea nuovo evento";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class BoolToNewEditLabelConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? "Nuovo operatore" : "Modifica operatore";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class BoolToPwdLabelConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? "Password *" : "Nuova password (lascia vuoto per non modificare)";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => CreateBrush(v as string, p as string);

    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();

    public static SolidColorBrush CreateBrush(string? hex, string? fallback = null)
    {
        var color = TryParseColor(hex) ?? TryParseColor(fallback) ?? Colors.Transparent;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    internal static System.Windows.Media.Color? TryParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        try
        {
            var value = System.Windows.Media.ColorConverter.ConvertFromString(hex.Trim());
            return value is System.Windows.Media.Color color ? color : null;
        }
        catch
        {
            return null;
        }
    }
}

public class HexToReadableForegroundConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        var color = HexToBrushConverter.TryParseColor(v as string)
            ?? HexToBrushConverter.TryParseColor(p as string)
            ?? Colors.Transparent;
        var luminance = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
        return luminance >= 160
            ? System.Windows.Media.Brushes.Black
            : System.Windows.Media.Brushes.White;
    }

    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class EqualityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        if (values.Length < 2)
            return false;

        var left = values[0];
        var right = values[1];

        if (left == DependencyProperty.UnsetValue || right == DependencyProperty.UnsetValue)
            return false;

        return Equals(left, right);
    }

    public object[] ConvertBack(object v, Type[] targetTypes, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class BoolToVoidLabelConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? "Riattiva scontrino" : "Annulla scontrino";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public class BoolToVoidStyleConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is true ? "AccentButton" : "DangerButton";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}
