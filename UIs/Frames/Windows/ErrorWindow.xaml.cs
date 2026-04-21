using System.Windows;
using System.Windows.Input;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    ///     ErrorWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public ErrorWindow(string errorName, string detailInfoText, List<string> troubleShootInfoTextList, string stackTraceText)
        {
            InitializeComponent();

            this.xErrorName.Text = errorName;
            this.xStackTrace.Text = stackTraceText;
            this.xDetailInfo.Text = detailInfoText;
            this.xTroubleShootInfo.Text = makeNumberingTextContent(troubleShootInfoTextList);

            PreviewKeyDown += errorWindow_PreviewKeyDown;
        }

        private static string makeNumberingTextContent(List<string> stringList)
        {
            if (stringList.Count == 0)
            {
                return "";
            }

            List<string> indexedList = stringList.Select((str, index) => $"{index + 1}. {str}").ToList();
            return string.Join(Environment.NewLine, indexedList);
        }

        private void errorWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}