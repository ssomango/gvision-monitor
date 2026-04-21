using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.Overlays;
using GVisionWpf.UIs.UiUpdaters;
using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Pages
{
    /// <summary>
    /// RunPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RunPage : Page
    {
        private readonly List<FloatingWindowBase> floatingWindows = new List<FloatingWindowBase>(16);
        private DetailResultWindow? detailResultWindow;  // 얘만 처음에 안 보였다가 device view 클릭하면 나타남

        private static bool isThisPageInitialized = false;

        private RunPageViewModel viewModel;

        public RunPage()
        {
            InitializeComponent();

            Loaded += loaded;
            Unloaded += unloaded;

            viewModel = new RunPageViewModel();
            DataContext = viewModel;
        }

        private void initializeFloatingWindows()
        {
            DeviceViewWindow prsDeviceViewWindow = new DeviceViewWindow("PRS Device View", PrsDeviceViewViewModel.Instance);
            SettingsViewModel.Instance.AddDeviceViewWindow(prsDeviceViewWindow);

            DeviceViewWindow mapDeviceViewWindow = new DeviceViewWindow("Mapping Device View", MapDeviceViewViewModel.Instance);
            SettingsViewModel.Instance.AddDeviceViewWindow(mapDeviceViewWindow);
           
            InspectionWindow prsWindow = new InspectionWindow("PRS Inspection");
            viewModel.PrsWindow = prsWindow;

            InspectionWindow mapWindow = new InspectionWindow("Mapping Inspection");
            viewModel.MapWindow = mapWindow;

            InspectionWindow topStripWindow = new InspectionWindow("Top Strip Inspection");
            viewModel.TopStripWindow = topStripWindow;

            //InspectionWindow bottomStripWindow = new InspectionWindow("Bottom Strip Inspection");
            //viewModel.BottomStripWindow = bottomStripWindow;


            this.detailResultWindow = new DetailResultWindow(DetailResultViewModel.Instance);

            var x1DeviceViewWindow = new DeviceViewWindow("X1 Picker Device View", X1PickerDeviceViewViewModel.Instance);
            SettingsViewModel.Instance.AddDeviceViewWindow(x1DeviceViewWindow);

            var x2DeviceViewWindow = new DeviceViewWindow("X2 Picker Device View", X2PickerDeviceViewViewModel.Instance);
            SettingsViewModel.Instance.AddDeviceViewWindow(x2DeviceViewWindow);

            this.floatingWindows.AddRange([
                prsDeviceViewWindow,
                mapDeviceViewWindow,
                prsWindow,
                mapWindow,
                topStripWindow,
                x1DeviceViewWindow,
                x2DeviceViewWindow,
                new ResultViewWindow("PRS Result View", PrsResultViewViewModel.Instance),
                new ResultViewWindow("Mapping Result View", MapResultViewViewModel.Instance)
            ]);
        }

        private void loaded(object sender, RoutedEventArgs e)
        {
            Window mainWindow = Application.Current.MainWindow!;

            ProgressWindow progressWindow = new ProgressWindow
            {
                Owner = mainWindow,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Top = mainWindow.Top,
                Left = mainWindow.Left,
                Width = mainWindow.Width,
                Height = mainWindow.Height,
            };
            progressWindow.Show();

            if (!isThisPageInitialized)
            {
                isThisPageInitialized = true;
                initializeFloatingWindows();
            }

            foreach (FloatingWindowBase window in this.floatingWindows)
            {
                window.Show();
            }

            progressWindow.Close();
        }

        private void unloaded(object sender, RoutedEventArgs e)
        {
            foreach (FloatingWindowBase window in this.floatingWindows)
            {
                window.Hide();
            }

            DetailResultViewModel.Instance.Visibility = Visibility.Hidden;
        }

        public IReadOnlyList<TutorSpotlightStep> BuildRunPageSpotlightSteps(int durationMs = 4500, int gapAfterMs = 0)
        {
            return new List<TutorSpotlightStep>
            {
                new("RUN_PANEL_ROOT", "Run Workspace", "RUN 모드 메인 작업 영역입니다. 실시간 검사 창과 결과 창을 함께 보며 운영합니다.", durationMs, gapAfterMs),

                new("RUN_PANEL_MAPPING_INSPECTION", "Mapping Inspection Window", "Mapping 검사 영상 창입니다. 맵 위치와 검사 진행 상태를 실시간으로 확인할 수 있습니다.", durationMs, gapAfterMs),
                new("RUN_PANEL_MAPPING_DEVICE_VIEW", "Mapping Device View","Mapping 디바이스 뷰입니다. 디바이스 그리드/셀 상태를 확인하고 맵핑 결과 분포를 빠르게 점검하세요.", durationMs, gapAfterMs), 
                new("RUN_PANEL_MAPPING_RESULT", "Mapping Result View", "매핑 결과 요약 창입니다. 평균 시간과 항목별 카운트를 보고 이상 징후를 빠르게 파악하세요.", durationMs, gapAfterMs),

                new("RUN_PANEL_PRS_INSPECTION", "PRS Inspection Window", "PRS 검사 영상 창입니다. 확대/축소와 오버레이를 확인하며 검사 상태를 모니터링하세요.", durationMs, gapAfterMs),
                new("RUN_PANEL_PRS_DEVICE_VIEW", "PRS Device View", "PRS 디바이스 뷰입니다. 디바이스 그리드 상태를 확인하고 셀 단위 변화를 점검하세요.", durationMs, gapAfterMs),
                new("RUN_PANEL_PRS_RESULT", "Prs Result View", "Prs 결과 요약 창입니다. 평균 시간과 항목별 카운트를 보고 이상 징후를 빠르게 파악하세요.", durationMs, gapAfterMs),

                new("RUN_PANEL_X1_PICKER_DEVICE_VIEW", "X1 Picker Device View", "X1 Picker 디바이스 뷰입니다. 픽커 관련 상태/그리드를 확인하고 동작 이상 여부를 점검하세요.", durationMs, gapAfterMs), 
                new("RUN_PANEL_X2_PICKER_DEVICE_VIEW", "X2 Picker Device View", "X2 Picker 디바이스 뷰입니다. 픽커 관련 상태/그리드를 확인하고 동작 이상 여부를 점검하세요.", durationMs, gapAfterMs),

                new("RUN_PANEL_TOP_STRIP_INSPECTION", "Top Strip Inspection", "Top Strip 검사 패널입니다. 스트립 검사 진행 상태와 주요 지표를 확인하세요.", durationMs, gapAfterMs),

                new("MAIN_BOTTOM_AREA", "Bottom Status Area", "메인 하단 상태 영역입니다. 현재 상태/로그/알림 등을 확인하고 운영 상태를 점검하세요.", durationMs, gapAfterMs),

                new("MAIN_SYSTEM_INFORMATION_PANEL", "System Information", "우측 하단 시스템 정보 패널입니다. 장비/통신/상태 정보를 확인하고 이상 징후를 빠르게 점검하세요.", durationMs, gapAfterMs),
            };
        }
    }
}
