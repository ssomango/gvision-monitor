using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class VisionWindowToolsToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EVisionWindowTools tool)
            {
                return tool switch
                {
                    EVisionWindowTools.ZoomIn => new TextBlock { Text = "Zoom In", FontSize = 16 },
                    EVisionWindowTools.ZoomOut => new TextBlock { Text = "Zoom Out", FontSize = 16 },
                    EVisionWindowTools.ZoomReset => new TextBlock { Text = "Zoom 100%", FontSize = 16 },
                    EVisionWindowTools.GrayValue => new TextBlock { Text = "Gray Value", FontSize = 16 },
                    EVisionWindowTools.DeviceView => new TextBlock { Text = "Device View", FontSize = 16 },
                    EVisionWindowTools.Overlay => new TextBlock { Text = "Inspection Overlay", FontSize = 16 },
                    EVisionWindowTools.Text => new TextBlock { Text = "Text On/Off", FontSize = 16 },
                    EVisionWindowTools.Ruler => new TextBlock { Text = "Ruler", FontSize = 16 },
                    EVisionWindowTools.LineProfile => new TextBlock { Text = "Line Profile", FontSize = 16 },
                    EVisionWindowTools.SaveImage => new TextBlock { Text = "Save Current Image", FontSize = 16 },
                    _ => throw new ArgumentException("Invalid EVisionWindowTools tool", nameof(tool))
                };
            }

            return new TextBlock();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
