using System.Globalization;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class StatisticsPanelYieldConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int total && values[1] is int good)
            {
                return total == 0 ? "0.00%" : $"{(double)good / total:P2}";
            }
            return "0.00%";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
