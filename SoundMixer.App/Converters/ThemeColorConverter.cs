using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SoundMixer.App.Converters;

public class PlayingToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isPlaying)
        {
            var playing = App.Current.FindResource("ClipPlayingBrush") as SolidColorBrush;
            var stopped = App.Current.FindResource("ClipStoppedBrush") as SolidColorBrush;
            return isPlaying
                ? (playing ?? new SolidColorBrush(Colors.Green))
                : (stopped ?? new SolidColorBrush(Colors.Purple));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}