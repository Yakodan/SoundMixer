using System.Globalization;
using System.Windows.Data;

namespace SoundMixer.App.Converters;

public class LevelToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float level)
        {
            float maxWidth = 280f;
            if (parameter is string paramStr && float.TryParse(paramStr, out float pw))
                maxWidth = pw;
            return (double)(Math.Min(level, 1f) * maxWidth);
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}