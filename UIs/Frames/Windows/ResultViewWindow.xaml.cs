using GVisionWpf.UIs.ViewModels;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// ResultViewWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ResultViewWindow : FloatingWindowBase
    {
        public ResultViewWindow(string windowName, ResultViewViewModel viewModel) : base(windowName)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}