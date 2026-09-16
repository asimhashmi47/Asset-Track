using System.Globalization;
using System.Windows.Data;

namespace AssetTrack.App.Converters;

/// <summary>ConverterParameter format: "Idle text|Busy text".</summary>
public class BusyTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter as string)?.Split('|') ?? ["Save", "Saving…"];
        var isBusy = value is true;
        return isBusy ? parts[1] : parts[0];
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
