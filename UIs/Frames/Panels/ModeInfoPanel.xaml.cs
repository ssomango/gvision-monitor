using GVisionWpf.UIs.ViewModels;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// ModeInfoPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModeInfoPanel : UserControl
    {
        public ModeInfoPanel()
        {
            InitializeComponent();
            DataContext = CurrentSettingViewmodel.Instance;
        }
    }
}
