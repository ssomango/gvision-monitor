using System.Text.RegularExpressions;
using System.Windows.Media;

namespace GVisionWpf.Utils
{
    public static class ColorConverter
    {
        public static string ToString(EColor color)
        {
            return Regex.Replace(color.ToString(), "(\\B[A-Z])", " $1").ToLower();
        }

        public static Brush ToBrush(this EColor color)
        {
            return color switch
            {
                EColor.Black => Brushes.Black,
                EColor.White => Brushes.White,
                EColor.Red => Brushes.Red,
                EColor.Green => Brushes.Green,
                EColor.Blue => Brushes.Blue,
                EColor.DimGray => Brushes.DimGray,
                EColor.Gray => Brushes.Gray,
                EColor.LightGray => Brushes.LightGray,
                EColor.Cyan => Brushes.Cyan,
                EColor.Magenta => Brushes.Magenta,
                EColor.Yellow => Brushes.Yellow,
                EColor.MediumSlateBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#7B68EE")),
                EColor.Coral => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF7F50")),
                EColor.SlateBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#6A5ACD")),
                EColor.SpringGreen => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FF7F")),
                EColor.OrangeRed => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4500")),
                EColor.DarkOliveGreen => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#556B2F")),
                EColor.Pink => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFC0CB")),
                EColor.CadetBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#5F9EA0")),
                EColor.Goldenrod => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#DAA520")),
                EColor.Orange => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFA500")),
                EColor.Gold => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700")),
                EColor.ForestGreen => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#228B22")),
                EColor.CornflowerBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#6495ED")),
                EColor.Navy => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#000080")),
                EColor.Turquoise => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#40E0D0")),
                EColor.DarkSlateBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#483D8B")),
                EColor.LightBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#ADD8E6")),
                EColor.IndianRed => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#CD5C5C")),
                EColor.VioletRed => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#D02090")),
                EColor.LightSteelBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#B0C4DE")),
                EColor.MediumBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#0000CD")),
                EColor.Khaki => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#F0E68C")),
                EColor.Violet => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#EE82EE")),
                EColor.Firebrick => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#B22222")),
                EColor.MidnightBlue => new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#191970")),
                _ => throw new ArgumentException("Invalid EColor value", nameof(color))
            };
        }
    }
}