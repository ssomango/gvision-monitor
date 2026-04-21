using GVisionWpf.UIs.ViewModels;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    public partial class StatisticsPanel : UserControl
    {
        private StatisticsPanelViewModel viewModel;
        public StatisticsPanel()
        {
            InitializeComponent();
            viewModel = new StatisticsPanelViewModel();
            DataContext = viewModel;
        }
    }
}
