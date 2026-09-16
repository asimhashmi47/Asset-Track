using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AssetTrack.Core.Enums;

namespace AssetTrack.App.Converters;

public class AssetStatusToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        AssetStatus.Available => "Available",
        AssetStatus.Assigned => "Assigned",
        AssetStatus.Repair => "Repair",
        AssetStatus.Scrap => "Scrap",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class AssetStatusToBrushConverter : IValueConverter
{
    /// <summary>ConverterParameter "Bg" or "Fg" selects which brush resource family to use.</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var suffix = parameter?.ToString() == "Fg" ? "Fg" : "Bg";
        var key = value switch
        {
            AssetStatus.Available => $"StatusAvailable{suffix}Brush",
            AssetStatus.Assigned => $"StatusAssigned{suffix}Brush",
            AssetStatus.Repair => $"StatusRepair{suffix}Brush",
            AssetStatus.Scrap => $"StatusScrap{suffix}Brush",
            _ => $"StatusAvailable{suffix}Brush"
        };
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
