using GVisionWpf.Api;
using GVisionWpf.Cameras;
using GVisionWpf.DSMMI.Frames;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.PresentationLayer.Communications;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Pages;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.Overlays;
using GVisionWpf.UIs.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using SystemUsageMonitorWindow = GVisionWpf.UIs.Frames.Windows.SystemUsageMonitorWindow;

namespace GVisionWpf
{
    public partial class MainWindow : Window
    {
        private SystemUsageMonitorWindow? systemWindow;
        readonly SetupPage setupPage;
        public Button OperationButton => xOperationButton;
        readonly RunPage runPage;
        private Point titlebarStartPos;
        // MainWindow 클래스 필드로 추가
        private int _walkthroughRunning; // 0 idle, 1 running
        private FloatingMenuWindow? floatingMenuWindow;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            // 이미지 삭제 타이머
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromHours(12);
            timer.Tick += DispatcherTimer_Tick;
            timer.Start();
            deleteImage();

            // 시작 시 조명 Off
            LightManager.Instance.TurnOffAllLightsFromAllCamera();

            this.setupPage = new SetupPage();
            this.runPage = new RunPage();
            this.xMainFrame.Navigate(this.setupPage);

            // 시작 시 자동 연결
            this.connectToHandler();

        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (floatingMenuWindow != null) return;

            floatingMenuWindow = new FloatingMenuWindow();
            floatingMenuWindow.LocationChanged += FloatingMenuWindow_LocationChanged;
            floatingMenuWindow.Owner = this;
            floatingMenuWindow.Show();
        }


        private void FloatingMenuWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (sender is not Window child) return;

            // 메인 윈도우 가져오기
            var parent = Application.Current.MainWindow;
            if (parent == null) return;

            // 부모 창 위치/크기
            var parentLeft = parent.Left;
            var parentTop = parent.Top;
            var parentRight = parent.Left + parent.Width;
            var parentBottom = parent.Top + parent.Height;

            // 자식 창 위치/크기
            var childLeft = child.Left;
            var childTop = child.Top;
            var childRight = child.Left + child.Width;
            var childBottom = child.Top + child.Height;

            // 왼쪽/위쪽 제한
            if (childLeft < parentLeft) child.Left = parentLeft;
            if (childTop < parentTop) child.Top = parentTop;

            // 오른쪽/아래쪽 제한
            if (childRight > parentRight) child.Left = parentRight - child.Width;
            if (childBottom > parentBottom) child.Top = parentBottom - child.Height;
        }


        // aml에서 추가
        public SetupPage SetupPage => setupPage;
        public string CurrentOperationMode => (xOperationButton.Content?.ToString() ?? string.Empty).Trim().ToUpperInvariant();
        public bool IsSetupMode => string.Equals(CurrentOperationMode, "SETUP", StringComparison.OrdinalIgnoreCase);
        public bool IsRunMode => string.Equals(CurrentOperationMode, "RUN", StringComparison.OrdinalIgnoreCase);

        public async void operationButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if ((string)this.xOperationButton.Content == "SETUP")
            {
                if (button?.Template.FindName("xOperationButtonImage", button) is Image image)
                {
                    image.Source = new BitmapImage(new Uri("/Assets/Icons/setup.png", UriKind.RelativeOrAbsolute));
                }
                LightManager.Instance.TurnOffAllLightsFromAllCamera();
                CameraManager.Instance.StopAllLiveSource();
                await Task.Delay(150);
                try
                {
                    CameraManager.Instance.SetRunMode();
                }
                catch (NotSupportedCameraException)
                {
                    GVisionMessenger.Instance.UI.SendSystemInfoMessage("지원하지 않는 카메라입니다.");
                }
                this.xOperationButton.Content = "RUN";
                this.xMainFrame.Navigate(this.runPage);

                this.connectToHandler();

                HistoryService.Instance.CreateHistory("RUN START", ELog.SystemLogs);
                GlobalSetting.Instance.CurrentRunningMode = ERunningMode.Run;
                Heart.Instance.CurrentVisionMode = EVisionMode.AutoRun;
            }
            else
            {
                bool? isChangeToSetup = new AlertWindow("Changing to Setup Mode", "Are you sure you want to switch to the Setup mode?", AlertWindow.EAlert.YESNO).ShowDialog();
                if (!isChangeToSetup.GetValueOrDefault())
                {
                    return;
                }

                if (button?.Template.FindName("xOperationButtonImage", button) is Image image)
                {
                    image.Source = new BitmapImage(new Uri("/Assets/Icons/run.png", UriKind.RelativeOrAbsolute));
                }

                try
                {
                    CameraManager.Instance.SetLiveMode();
                }
                catch (NotSupportedCameraException)
                {
                    GVisionMessenger.Instance.UI.SendSystemInfoMessage("지원하지 않는 카메라입니다.");
                }
                this.xOperationButton.Content = "SETUP";
                this.xMainFrame.Navigate(this.setupPage);

                HistoryService.Instance.CreateHistory("SETUP START", ELog.SystemLogs);
                GlobalSetting.Instance.CurrentRunningMode = ERunningMode.SetUp;
                Heart.Instance.CurrentVisionMode = EVisionMode.Teaching;
            }
        }

        public bool EnsureOperationMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return false;
            }

            var desiredMode = mode.Trim().ToUpperInvariant();
            if (desiredMode != "RUN" && desiredMode != "SETUP")
            {
                return false;
            }

            if (string.Equals(CurrentOperationMode, desiredMode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            operationButton_OnClick(xOperationButton, new RoutedEventArgs());
            return true;
        }

        public FrameworkElement? ResolveAnchorElement(string anchorKey)
        {
            return TutorAnchorResolver.Resolve(this, anchorKey);
        }

        private void connectToHandler()
        {
            this.xConnectionBtn.Content = "CONNECTED";
            this.xConnectionBtn.Background = Brushes.Green;
            Communicator.Instance.Connect();
        }

        private void disconnectToHandler()
        {
            this.xConnectionBtn.Content = "DISCONNECTED";
            this.xConnectionBtn.Background = Brushes.Red;
            Communicator.Instance.ReleaseClient();
        }

        private void connectionBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if ((string)this.xConnectionBtn.Content == "DISCONNECTED")
            {
                connectToHandler();
            }
            else
            {
                if ((string)this.xOperationButton.Content == "RUN")
                {
                    new AlertWindow("Notification", AlertWindow.EIcon.ALERT, "In run mode, The connection cannot be disconnected.", AlertWindow.EAlert.YES).ShowDialog();
                    return;
                }
                disconnectToHandler();
            }
        }

        private void logoImage_OnClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            bool? answer = new AlertWindow(
                "Caution!!",
                "Please enter test mode only when no inspections are in progress.\n Continuing could interfere with ongoing inspections.\n Do you want to proceed?",
                AlertWindow.EAlert.YESNO).ShowDialog();
            if (answer == true)
                new VinsFantasyWindow().Show();
#endif
        }

        private void titleBarLabel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.titlebarStartPos = e.GetPosition(null);
            }
        }

        private void titleBarLabel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (WindowState == WindowState.Maximized && Math.Abs(this.titlebarStartPos.Y - e.GetPosition(null).Y) > 2)
            {
                var point = PointToScreen(e.GetPosition(null));
                WindowState = WindowState.Normal;
                Left = point.X - ActualWidth / 2;
                Top = point.Y - this.xMainWindowBorder.ActualHeight / 2;
            }

            DragMove();

            if (Left < 0) Left = 0;
            if (Top < 0)
            {
                Top = 0;
                WindowState = WindowState.Maximized;
            }
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            deleteImage();
        }

        private void deleteImage()
        {
            int saveDays = GlobalSetting.Instance.Inspection.SaveDays;
            DirectoryInfo dirInfo = new DirectoryInfo("DB/Images");
            DateTime cmpTime = DateTime.Now.AddDays(-saveDays);
            bool imageDeleted = false;

            foreach (DirectoryInfo dir in dirInfo.GetDirectories())
            {
                if (dir.CreationTime < cmpTime)
                {
                    dir.Delete(true);
                    imageDeleted = true;
                }
            }

            if (!imageDeleted) return;

            HistoryService.Instance.CreateHistory("The image has been deleted by the auto deletion policy.", ELog.SystemLogs);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            bool isMaximized = this.WindowState == WindowState.Maximized;
            if (isMaximized)
            {
                this.Left = 0;
                this.Top = 0;
            }
        }

        public void ShowMainWindowSpotlight(string anchorKey, string? title, string? body, int durationMs = 4500)
        {
            this.UpdateLayout();

            var target = TutorAnchorResolver.Resolve(this, anchorKey);
            if (target == null)
            {
                return;
            }

            TutorSpotlightOverlay.Show(
                hostWindow: this,
                target: target,
                title: title,
                body: body,
                durationMs: durationMs,
                padding: 10,
                dimOpacity: 0.62);
        }

        public IReadOnlyList<TutorSpotlightStep> BuildSetupWalkthroughSteps(int durationMs = 4500, int gapAfterMs = 0)
        {
            return new List<TutorSpotlightStep>
            {
                new("MAIN_MAINFRAME", "Setup Workflow Overview", "현재 화면은 Setup 작업 영역입니다. 왼쪽 사이드바 탭을 순서대로 열고 각 메뉴에서 설정/점검을 진행할 수 있습니다.", durationMs, gapAfterMs),

                // TEACHING
                new(
                    anchorKey: "SIDEBAR_TAB_TEACHING",
                    title: "TEACHING Tab (Expand)",
                    body: "자재별 티칭을 할 수 있는 메뉴입니다. 왼쪽 TEACHING 헤더를 클릭해 펼친 뒤 자재 타입을 선택하여 티칭하세요.",
                    durationMs: durationMs,
                    gapAfterMs: gapAfterMs,
                    isGateStep: true,
                    continueCondition: _ => IsSidebarExpanded("TEACHING"),
                    pollIntervalMs: 80),
                new("MENU_MAPPING", "TEACHING - MAPPING", "맵 기반 티칭 메뉴입니다. 클릭 후 위치/정렬 기준을 설정해 자동 검사 흐름을 준비하세요.", durationMs, gapAfterMs),
                new("MENU_BGA", "TEACHING - BGA", "BGA 레시피 티칭 메뉴입니다. 클릭해 ROI/파라미터를 학습하고 결과를 저장할 수 있습니다.", durationMs, gapAfterMs),
                new("MENU_LGA", "TEACHING - LGA", "LGA 레시피 티칭 메뉴입니다. 클릭 후 기준 패턴과 검사 조건을 등록해 다음 검사에 사용하세요.", durationMs, gapAfterMs),
                new("MENU_QFN", "TEACHING - QFN", "QFN 레시피 티칭 메뉴입니다. 클릭해 항목별 조건을 조정하고 검증 실행으로 품질을 확인하세요.", durationMs, gapAfterMs),
                
                
                // SPC
                new(
                    anchorKey: "SIDEBAR_TAB_SPC",
                    title: "SPC Tab (Expand)",
                    body: "검사 이력/통계 메뉴 그룹입니다. 왼쪽 SPC 헤더를 클릭해 펼친 뒤 분석 메뉴를 선택하세요.",
                    durationMs: durationMs,
                    gapAfterMs: gapAfterMs,
                    isGateStep: true,
                    continueCondition: _ => IsSidebarExpanded("SPC"),
                    pollIntervalMs: 80),
                new("MENU_HISTORY", "SPC - History", "검사 결과 이력 조회 메뉴입니다. 클릭해 기간/조건으로 검색하고 불량 추이를 확인하세요.", durationMs, gapAfterMs),
                new("MENU_LOT_DATA", "SPC - Lot Data", "LOT 단위 데이터 분석 메뉴입니다. 클릭해 LOT별 수율과 상세 결과를 비교할 수 있습니다.", durationMs, gapAfterMs),

                // SETUP
                new(
                    anchorKey: "SIDEBAR_TAB_SETUP",
                    title: "SETUP Tab (Expand)",
                    body: "기본 설정 메뉴 그룹입니다. 왼쪽 SETUP 헤더를 클릭해 펼친 뒤 하위 메뉴를 확인하세요.",
                    durationMs: durationMs,
                    gapAfterMs: gapAfterMs,
                    isGateStep: true,
                    continueCondition: _ => IsSidebarExpanded("SETUP"),
                    pollIntervalMs: 80),
                new("MENU_SETTINGS", "SETUP - Settings", "시스템 기본 동작을 설정하는 메뉴입니다. 클릭해 설정 창으로 이동하고 저장까지 진행할 수 있습니다.", durationMs, gapAfterMs),
                new("MENU_CALIBRATION", "SETUP - Calibration", "장비/비전 보정 메뉴입니다. 클릭 후 탭별 보정값을 조정하고 적용 상태를 확인하세요.", durationMs, gapAfterMs),
                new("MENU_LIGHT", "SETUP - Light", "조명 채널 제어 메뉴입니다. 클릭해 밝기를 조정하고 검사 조건에 맞게 저장할 수 있습니다.", durationMs, gapAfterMs),
                
                
                // SYSTEM
                new(
                    anchorKey: "SIDEBAR_TAB_SYSTEM",
                    title: "SYSTEM Tab (Expand)",
                    body: "시스템 운영 메뉴 그룹입니다. 왼쪽 SYSTEM 헤더를 클릭해 펼친 뒤 관리 기능을 선택하세요.",
                    durationMs: durationMs,
                    gapAfterMs: gapAfterMs,
                    isGateStep: true,
                    continueCondition: _ => IsSidebarExpanded("SYSTEM"),
                    pollIntervalMs: 80),
                new("MENU_MONITOR", "SYSTEM - Monitor", "리소스/상태 모니터 메뉴입니다. 클릭해 CPU/메모리와 장비 상태를 실시간으로 확인하세요.", durationMs, gapAfterMs),
                new("MENU_AS", "SYSTEM - A/S", "유지보수 지원 메뉴입니다. 클릭해 점검 도구를 열고 장애 대응 작업을 진행할 수 있습니다.", durationMs, gapAfterMs),
                new("MENU_EXIT", "SYSTEM - Exit", "프로그램 종료 메뉴입니다. 작업 저장 후 클릭하면 애플리케이션을 안전하게 종료할 수 있습니다.", durationMs, gapAfterMs),
            };
        }

        public IReadOnlyList<TutorSpotlightStep> BuildRunWalkthroughSteps(int durationMs = 4500, int gapAfterMs = 0)
        {
            return this.runPage.BuildRunPageSpotlightSteps(durationMs, gapAfterMs);
        }

        public IReadOnlyList<TutorSpotlightStep> BuildFullWalkthroughSteps(int durationMs = 4500, int gapAfterMs = 0)
        {
            var steps = new List<TutorSpotlightStep>();
            steps.AddRange(BuildSetupWalkthroughSteps(durationMs, gapAfterMs));
            steps.Add(new TutorSpotlightStep(
                anchorKey: "MAIN_OPERATION_BUTTON",
                title: "SETUP/RUN Toggle (Click)",
                body: "모드 전환 버튼입니다. 왼쪽 상단 SETUP/RUN 토글을 클릭해 RUN 화면으로 이동하고 실시간 검사 안내를 계속하세요.",
                durationMs: durationMs,
                gapAfterMs: gapAfterMs,
                isGateStep: true,
                continueCondition: _ => IsRunModeActivated(),
                pollIntervalMs: 80));
            steps.AddRange(BuildRunWalkthroughSteps(durationMs, gapAfterMs));
            return steps;
        }

        public IReadOnlyList<TutorSpotlightStep> BuildMainWindowSpotlightSteps(int durationMs = 4500, int gapAfterMs = 0)
        {
            return BuildFullWalkthroughSteps(durationMs, gapAfterMs);
        }

        private bool IsRunModeActivated()
        {
            UpdateLayout();
            var operationText = (xOperationButton.Content?.ToString() ?? string.Empty).Trim();
            var isRunButtonState = operationText.Equals("RUN", StringComparison.OrdinalIgnoreCase);
            var isRunPageLoaded = xMainFrame.Content is RunPage;
            return isRunButtonState && isRunPageLoaded;
        }

        private bool IsSidebarExpanded(string headerText)
        {
            foreach (var exp in FindVisualChildren<Expander>(this))
            {
                var header = (exp.Header?.ToString() ?? string.Empty).Trim();
                if (!header.Equals(headerText, StringComparison.OrdinalIgnoreCase)) continue;
                if (exp.IsExpanded) return true;
            }
            return false;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T typed)
                    yield return typed;

                foreach (var nested in FindVisualChildren<T>(child))
                    yield return nested;
            }
        }

        public async Task RunMainWindowSpotlightWalkthroughAsync(
            int durationMs = 4500,
            int gapAfterMs = 0,
            string? requestId = null,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _walkthroughRunning, 1) == 1)
            {
                Debug.WriteLine($"[Tutor/MainWindow] Walkthrough already running - ignored rid={requestId}");
                return;
            }

            try
            {
                var steps = BuildFullWalkthroughSteps(durationMs, gapAfterMs);
                await TutorWalkthroughService.RunAsync(this, steps, requestId, cancellationToken);
            }
            finally
            {
                Interlocked.Exchange(ref _walkthroughRunning, 0);
            }
        }
    }
}
