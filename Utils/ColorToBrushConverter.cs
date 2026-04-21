using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GVisionWpf.Utils
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush brush = Brushes.LightGray;
            if (value is EColor color)
            {
                return ColorConverter.ToBrush(color);
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
