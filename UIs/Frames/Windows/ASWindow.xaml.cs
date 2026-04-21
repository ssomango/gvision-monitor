using System.Windows;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// ASWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ASWindow : Window
    {
        public ASWindow()
        {
            InitializeComponent();
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
