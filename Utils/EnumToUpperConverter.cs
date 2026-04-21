using System.Globalization;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class EnumToUpperConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Enum ? value.ToString()?.ToUpper() : value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}