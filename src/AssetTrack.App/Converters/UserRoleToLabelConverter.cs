using System.Globalization;
using System.Windows.Data;
using AssetTrack.Core.Enums;

namespace AssetTrack.App.Converters;

/// <summary>Display-only relabeling: the Staff role reads "Standard" in the UI; the underlying
/// permission model (Admin vs Staff) is unchanged.</summary>
public class UserRoleToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        UserRole.Admin => "Admin",
        UserRole.Staff => "Standard",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
