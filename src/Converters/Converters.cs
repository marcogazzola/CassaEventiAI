using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CplCassaEventi.Converters;

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
