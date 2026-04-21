using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace GVisionWpf.Utils
{
    public class CameraPanelToolsToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ECameraPanelTools tool)
            {
                return tool switch
                {
                    ECameraPanelTools.ZoomIn => new TextBlock { Text = "Zoom In", FontSize = 16 },
                    ECameraPanelTools.ZoomOut => new TextBlock { Text = "Zoom Out", FontSize = 16 },
                    ECameraPanelTools.ZoomReset => new TextBlock { Text = "Zoom 100%", FontSize = 16 },
                    ECameraPanelTools.Stop => new TextBlock { Text = "Live Stop", FontSize = 16 },
                    ECameraPanelTools.Start => new TextBlock { Text = "Live Start", FontSize = 16 },
                    ECameraPanelTools.Save => new TextBlock { Text = "Save Image", FontSize = 16 },
                    ECameraPanelTools.Menu => new TextBlock { Text = "Menu", FontSize = 16 },
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
