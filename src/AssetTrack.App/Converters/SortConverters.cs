using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>Visible when the bound SortBy column name equals the ConverterParameter (this column's key).</summary>
public class SortIndicatorVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Renders ▲ when ascending, ▼ when descending.</summary>
public class SortDirectionArrowConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "▲" : "▼";

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
