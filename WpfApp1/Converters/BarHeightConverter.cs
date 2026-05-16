using System.Globalization;
using System.Windows.Data;

namespace PharmacyApp.Converters;

public class BarHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double amount = value switch
        {
            decimal dec => (double)dec,
            double d => d,
            int i => i,
            float f => f,
            _ => 0
        };
        return Math.Max(24, Math.Min(110, amount / 35));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
