using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.UIs.Frames.Panels;
using GVisionWpf.UIs.ViewModels;
using GVisionWpf.UIs.ViewModels.Calibrations;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Point = System.Windows.Point;
using Window = System.Windows.Window;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// CalibrationWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
 
    public partial class CalibrationWindow : Window
    {
        private Point titlebarStartPos;

        private readonly CalibrationViewModel calibrationViewModel = CalibrationViewModel.Instance;
        
        public CalibrationWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            DataContext = this.calibrationViewModel;
            this.calibrationViewModel.LoadLightRecipe();

            CurrentTeachingWindow.Instance.Window = this.xVisionWindow;

            this.calibrationViewModel.ScrollToNewItemRequested += (_, item) => { this.listView.ScrollIntoView(item); };

            Closing += calibrationWindow_Closing;
        }
        public CalibrationViewModel ViewModel => this.DataContext as CalibrationViewModel;

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            this.calibrationViewModel.IsWindowOpened = true;

            // 모든 live observer 제거 (박사님: 라이브 해야함 vs 윤빈: 라이브 안됨)
            /*foreach (var cameraInfo in GlobalSetting.Instance.CameraInfos)
            {
                CameraManager.Instance.Cameras[cameraInfo.CameraType].ClearLiveObservers();
            }*/
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            LightManager.Instance.TurnOffAllLightsFromAllCamera();
            Close();
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.calibrationViewModel.LightControllerCollection.Clear();

            this.calibrationViewModel.LoadDataGridItems();

            if (this.calibrationViewModel.DataGridItems.Count == 0)
            {
                return;
            }

            this.calibrationViewModel.CameraSelectedIndex = 0;
        }

        private void calibrationWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            this.calibrationViewModel.IsWindowOpened = false;
        }

        private void xTitleBarLabel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.titlebarStartPos = e.GetPosition(null);
            }
        }

        private void xTitleBarLabel_PreviewMouseMove(object sender, MouseEventArgs e)
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
                Top = point.Y - this.xPickerOffsetWindowGrid.ActualHeight / 2;
            }

            DragMove();
        }
        //필요 X
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}