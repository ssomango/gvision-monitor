using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.LightBlue;
        public Brush FalseBrush { get; set; } = Brushes.LightGray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? TrueBrush : FalseBrush;

            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}