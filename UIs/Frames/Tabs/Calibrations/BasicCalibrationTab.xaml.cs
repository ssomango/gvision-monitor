using GVisionWpf.UIs.ViewModels.Calibrations;

namespace GVisionWpf.UIs.Frames.Tabs.Calibrations
{
    /// <summary>
    /// DefaultCalibrationTab.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BasicCalibrationTab : CalibrationTabBase
    {
        public BasicCalibrationTab()
        {
            InitializeComponent();

            //aml
            this.Loaded += (s, e) =>
            {
                if (this.DataContext is CalibrationTabViewModel vm)
                {
                    vm.RoiPanel = this.xRoiPanel; // 연결
                    vm.ThresholdPanel = this.xThresholdPanel;
                }
            };
        }

        public override void OnAppear()
        {
            this.xRoiPanel.Attach();
        }

        public override void OnDisappear()
        {
            this.xRoiPanel.Detach();
        }
        //필요없을듯
        private void xRoiPanel_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}