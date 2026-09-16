using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>Visible when the bound string is non-empty (e.g. show an error banner only when set).</summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
