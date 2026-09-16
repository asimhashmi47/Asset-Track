using System.Globalization;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>Maps a 0..1 fraction to a pixel width for a simple inline bar chart.</summary>
public class FractionToWidthConverter : IValueConverter
{
    public static readonly FractionToWidthConverter Instance = new();
    private const double MaxWidth = 150;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d ? Math.Max(4, d * MaxWidth) : 4.0;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
