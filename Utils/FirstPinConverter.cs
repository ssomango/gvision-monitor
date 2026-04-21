using System.Globalization;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class FirstPinConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is EFirstPin && parameter is EFirstPin)
            {
                return value.Equals(parameter);
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is true && parameter is EFirstPin)
            {
                return parameter;
            }
            return Binding.DoNothing;
        }
    }
}