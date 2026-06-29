using System.Globalization;
using System.Windows.Data;

namespace SoundMixer.App.Helpers;

public class ThemeDisplayConverter : IValueConverter
{
    public static readonly ThemeDisplayConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string themeName)
            return ThemeManager.GetDisplayName(themeName);
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}