using GVisionWpf.UIs.ViewModels;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// SystemInformationPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SystemInformationPanel : UserControl
    {
        private readonly DispatcherTimer scrollTimer;

        private SystemInformationViewModel viewModel;

        public SystemInformationPanel()
        {
            InitializeComponent();
            viewModel = new SystemInformationViewModel();

            DataContext = viewModel;

            this.scrollTimer = new DispatcherTimer();
            this.scrollTimer.Interval = TimeSpan.FromMilliseconds(100);
            this.scrollTimer.Tick += scrollTimer_Tick;
            this.scrollTimer.Start();
        }

        private void scrollTimer_Tick(object? sender, EventArgs e)
        {
            if (!viewModel.HasNewItems)
            {
                return;
            }

            string lastItem = viewModel.Logs[^1];
            this.listView.ScrollIntoView(lastItem);
            viewModel.HasNewItems = false;
        }
    }
}
