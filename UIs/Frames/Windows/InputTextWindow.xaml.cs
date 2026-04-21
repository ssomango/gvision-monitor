using System.Windows;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// InputTextWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InputTextWindow : Window
    {
        public InputTextWindow()
        {
            InitializeComponent();
        }

        #region Custom

        public InputTextWindow(string label)
        {
            InitializeComponent();
            this.xLabel.Content = label;
        }

        public InputTextWindow(string title, string label)
        {
            InitializeComponent();
            this.xTitle.Content = title;
            this.xLabel.Content = label;
        }
        public InputTextWindow(string title, string label, string before)
        {
            InitializeComponent();
            this.xTitle.Content = title;
            this.xLabel.Content = label;
            this.xTextBox.Text = before;
        }

        #endregion

        private void ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
