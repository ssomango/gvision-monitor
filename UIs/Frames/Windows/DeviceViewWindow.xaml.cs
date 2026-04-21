using GVisionWpf.Models.Entities.Result;
using GVisionWpf.UIs.ViewModels;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// DeviceViewWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DeviceViewWindow : FloatingWindowBase
    {
        public DeviceViewWindow(string windowName, DeviceViewViewModel viewModel) : base(windowName)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}