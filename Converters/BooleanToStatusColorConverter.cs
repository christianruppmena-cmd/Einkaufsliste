using System.Globalization;

namespace Einkaufsliste.Converters;

public class BooleanToStatusColorConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is bool istAktiv)
            return istAktiv
                ? Color.FromArgb("#1B5E43")
                : Color.FromArgb("#D32F2F");

        return Colors.Gray;
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