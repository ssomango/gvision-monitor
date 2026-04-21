using System.Globalization;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class CalibrationSelectConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is EShape && parameter is EShape)
            {
                return value.Equals(parameter);
            }
            if (value is ECalibrationStandard && parameter is ECalibrationStandard)
            {
                return value.Equals(parameter);
            }
            if (value is EReticleType && parameter is EReticleType)
            {
                return value.Equals(parameter);
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is true && parameter is EShape)
            {
                return parameter;
            }
            if (value is true && parameter is ECalibrationStandard)
            {
                return parameter;
            }
            if (value is true && parameter is EReticleType)
            {
                return parameter;
            }
            return Binding.DoNothing;
        }
    }
}