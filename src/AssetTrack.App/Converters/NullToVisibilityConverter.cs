using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>Visible when the bound value is non-null (any type — DateTime?, object, etc.).</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
