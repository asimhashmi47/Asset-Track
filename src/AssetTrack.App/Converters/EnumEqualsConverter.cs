using System.Globalization;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>Used to bind a RadioButton's IsChecked to "does this enum property equal this literal".</summary>
public class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();

    public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b || !b || parameter is null)
            return Binding.DoNothing;

        var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Enum.Parse(enumType, parameter.ToString()!);
    }
}
