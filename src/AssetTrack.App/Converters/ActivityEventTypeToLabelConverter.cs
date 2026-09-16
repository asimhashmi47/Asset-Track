using System.Globalization;
using System.Windows.Data;
using AssetTrack.Core.Enums;

namespace AssetTrack.App.Converters;

public class ActivityEventTypeToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ActivityEventType.AssetCreated => "Created",
        ActivityEventType.Allocated => "Allocated",
        ActivityEventType.Returned => "Returned",
        ActivityEventType.MarkedRepair => "Marked repair",
        ActivityEventType.MarkedScrap => "Marked scrap",
        ActivityEventType.UserCreated => "User created",
        ActivityEventType.AssetUpdated => "Details updated",
        ActivityEventType.UserUpdated => "Details updated",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
