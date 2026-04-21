using GVisionWpf.Models.Entities.Result;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GVisionWpf.Utils
{
    class InspectionResultTooltipConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string defaultValue = "The inspection result does not exist.";
            if (value is InspectionResult item)
            {
                return CreateToolTipContent(item.ToString() ?? defaultValue);
            }
            return defaultValue;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected UIElement CreateToolTipContent(string inspectionResult)
        {
            var stackPanel = new StackPanel();

            string[] lines = inspectionResult.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                bool isBold = line.Contains("NG");
                bool isRed = line.Contains("NG");

                AddStyledTextBlock(stackPanel, line, isBold, isRed);
            }

            return stackPanel;
        }

        protected void AddStyledTextBlock(Panel panel, string text, bool isBold = false, bool isRed = false)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isRed ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black)
            };

            panel.Children.Add(textBlock);
        }
    }
}
