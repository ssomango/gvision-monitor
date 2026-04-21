using System.Windows;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// ProgressWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
            ShowInTaskbar = false;
        }
    }
}
