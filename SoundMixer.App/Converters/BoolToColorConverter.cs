using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SoundMixer.App.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isTrue && isTrue)
            return new SolidColorBrush(Color.FromRgb(29, 185, 84));
        return new SolidColorBrush(Color.FromRgb(108, 99, 255));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}