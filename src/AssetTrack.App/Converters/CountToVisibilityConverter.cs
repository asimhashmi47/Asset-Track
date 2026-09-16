using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>Visible when the bound count is zero. ConverterParameter "Invert" flips that.</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value is int i ? i : 0;
        var isZero = count == 0;
        if (parameter?.ToString() == "Invert")
            isZero = !isZero;
        return isZero ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
