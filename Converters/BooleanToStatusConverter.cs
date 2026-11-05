using System.Globalization;

namespace Einkaufsliste.Converters;

public class BooleanToStatusConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is bool istAktiv)
            return istAktiv ? "Aktiv" : "Inaktiv";

        return "Unbekannt";
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}