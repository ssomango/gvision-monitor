using GVisionWpf.UIs.ViewModels;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// LotInfoPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LotInfoPanel : UserControl
    {
        public LotInfoPanel()
        {
            InitializeComponent();
            DataContext = CurrentSettingViewmodel.Instance;
        }
    }
}
