using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class AngleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            Debug.WriteLine($"[Converter] value={value}, parameter={parameter}");
            if (parameter != null && int.TryParse(parameter.ToString(), out int angleParam) &&
                value != null && int.TryParse(value.ToString(), out int angleValue))
            {
                if (angleParam == 0 || angleParam == 90 || angleParam == 180 || angleParam == 270)
                {
                    return angleValue == angleParam;
                }
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // When the RadioButton is checked, return the parameter as the rotation angle
            if (value is true && parameter != null && int.TryParse(parameter.ToString(), out int angle))
            {
                return angle;
            }
            return Binding.DoNothing;
        }
    }
}