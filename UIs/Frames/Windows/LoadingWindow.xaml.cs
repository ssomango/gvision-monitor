using GVisionWpf.Cameras;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Services;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// LoadingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private Point titlebarStartPos;
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();

        public LoadingWindow()
        {
            InitializeComponent();
        }

        #region Events

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q)
            {
                cancelDelay();
            }
        }

        private void xPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.titlebarStartPos = e.GetPosition(null);
            }
        }

        private void xPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (WindowState == WindowState.Maximized && Math.Abs(this.titlebarStartPos.Y - e.GetPosition(null).Y) > 2)
            {
                Point point = PointToScreen(e.GetPosition(null));

                WindowState = WindowState.Normal;

                Left = point.X - ActualWidth / 2;
                Top = point.Y - this.xLoadingWindowGrid.ActualHeight / 2;
            }

            DragMove();
        }

        #endregion

        public async Task LoadSystem(int minDelay)
        {
            try
            {
                HOperatorSet.SetSystem("width", 4096);
                HOperatorSet.SetSystem("height", 4096);
                HOperatorSet.SetSystem("use_window_thread", "true");

            }
            catch
            {
                throw new HalconLicenseException();
            }

            Task delayTask = Task.Delay(minDelay, cts.Token);

            Task applySettingTask = Task.Run(() =>
            {
                displayMessage("설정 파일을 로딩합니다.");
                GlobalSetting.Instance.ApplySetting();
                displayMessage("설정 파일 로딩이 완료되었습니다.");
            });

            Task loadCameraTask = Task.Run(() =>
            {
                displayMessage("카메라 초기화를 시작합니다.");
                CameraManager.Instance.InitializeAllCamera();
                displayMessage("카메라 초기화가 끝났습니다.");
            });

            HistoryService.Instance.CreateHistory("GVISION START", ELog.SystemLogs);

            try
            {
                await delayTask;
            }
            catch
            {
                // ignored
            }

            await Task.WhenAll(applySettingTask, loadCameraTask);
        }

        private void displayMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                this.xMessageLabel.Content = message;
            });
        }

        private static void cancelDelay()
        {
            cts.Cancel();
        }
    }
}