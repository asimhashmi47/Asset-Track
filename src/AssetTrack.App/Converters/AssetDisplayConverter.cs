using System.Globalization;
using System.Windows.Data;
using AssetTrack.Core.Entities;

namespace AssetTrack.App.Converters;

public class AssetDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Asset a ? $"{a.AssetTag} — {a.Name} ({a.SerialNumber})" : string.Empty;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
