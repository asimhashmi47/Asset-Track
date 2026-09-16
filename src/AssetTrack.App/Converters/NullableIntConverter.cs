using System.Globalization;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>Lets a TextBox bind directly to an int? — empty text becomes null instead of a validation error.</summary>
public class NullableIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() ?? string.Empty;

    public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text)) return null;
        return int.TryParse(text, out var result) ? result : Binding.DoNothing;
    }
}
