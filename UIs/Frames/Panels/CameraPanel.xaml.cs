using GVisionWpf.Cameras;
using GVisionWpf.Illuminations;
using GVisionWpf.UIs.UiUpdaters;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// CameraPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CameraPanel : UserControl, ILiveObserver
    {
        private ILiveFrameProcessor frameProcessor;
        private ECamera sourceCameraType;
        private bool isFirstImage = true;
        private LightManager lightManager;
        private readonly Dictionary<string, ILiveFrameProcessor> frameProcessors;

        #region Property

        public ECamera SourceCameraType
        {
            get => this.sourceCameraType;
            set
            {
                this.sourceCameraType = value;
                this.xCameraComboBox.SelectedItem = value;
                foreach ((_, ILiveFrameProcessor liveFrameProcessor) in this.frameProcessors)
                {
                    liveFrameProcessor.SetCameraType(value);
                }
            }
        }

        public ILiveFrameProcessor FrameProcessor
        {
            get => this.frameProcessor;
            set
            {
                StopLive();
                this.frameProcessor = value;
            }
        }

        #endregion

        public CameraPanel()
        {
            InitializeComponent();
            Loaded += onLoaded;

            this.lightManager = LightManager.Instance;

            this.sourceCameraType = ECamera.NotSelected;
            this.xCameraComboBox.ItemsSource = Enum.GetValues(typeof(ECamera));
            this.frameProcessors = new Dictionary<string, ILiveFrameProcessor>();

            populateCameraOptions();
            this.frameProcessor = this.frameProcessors["None"];
        }

        private void populateCameraOptions()
        {
            this.frameProcessors.Add("None", new LiveDefaultProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("Reticle", new LiveReticleProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("Full Size Reticle", new LiveFullSizeReticleProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("Histogram", new LiveHistogramProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("Mapping Grid ROI", new LiveMappingGridRoiProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("Bottom Jig Offset", new LiveBottomJigOffsetProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("Setting Jig Offset", new LiveSettingJigOffsetProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("Picker Offset", new LivePickerProcessor(this.sourceCameraType, this.xHSmartWindow));
            this.frameProcessors.Add("PRS Offset", new LivePrsOffsetProcessor(this.sourceCameraType, this.xHSmartWindow));

            ObservableCollection<string> cameraOptions = new ObservableCollection<string>();
            foreach ((string key, _) in this.frameProcessors)
            {
                cameraOptions.Add(key);
            }

            this.xCameraOptions.ItemsSource = cameraOptions;
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            const int ONE_MB_IN_BYTE = 1024 * 1024;
            const int MAX_STACK_MEM_SIZE_IN_BYTE = 75 * ONE_MB_IN_BYTE;

            this.xHSmartWindow.HalconWindow.SetWindowParam("graphics_stack_max_element_num", 50);
            this.xHSmartWindow.HalconWindow.SetWindowParam("graphics_stack_max_memory_size", MAX_STACK_MEM_SIZE_IN_BYTE);
        }

        private void changeCamera(ECamera selectedCamera)
        {
            StopLive();
            SourceCameraType = selectedCamera;
            this.isFirstImage = true;
        }

        public void StartLive()
        {
            StopLive();
            CameraManager.Instance.Cameras[this.sourceCameraType].AddLiveObserver(this);
        }

        public void StopLive()
        {
            // Debug.WriteLine("끄기~!!!!!!");
            CameraManager.Instance.Cameras[this.sourceCameraType].RemoveLiveObserver(this);
        }

        public void UpdateFrame(HObject image)
        {
            this.frameProcessor.Display(image);

            if (!this.isFirstImage)
            {
                return;
            }

            FitImage();
            this.isFirstImage = false;
        }

        public void FitImage()
        {
            Dispatcher.Invoke(() => { this.xHSmartWindow.SetFullImagePart(); });
        }

        private void saveImage()
        {
            CameraManager.Instance.WriteImage(SourceCameraType);
        }

        public void ZoomImage(int delta)
        {
            System.Windows.Size windowSize = this.xHSmartWindow.WindowSize;

            double x = windowSize.Width / 2;
            double y = windowSize.Height / 2;

            this.xHSmartWindow.HZoomWindowContents(x, y, delta);
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            FrameProcessor = new LiveDefaultProcessor(this.sourceCameraType, this.xHSmartWindow);
            StartLive();
        }

        private async void stopButton_Click(object sender, RoutedEventArgs e)
        {
            this.frameProcessor = this.frameProcessors["None"];
            this.xCameraOptions.SelectedIndex = 0;

            // Stop Live는 Observer의 구독을 취소하는 것입니다. 카메라 라이브는 다른 Task에서 돌죠.
            // ClearWindow() 이후 UpdateFrame()이 호출될 수 있으니 조금 기다립시다.
            StopLive();
            await Task.Delay(300);
            this.xHSmartWindow.HalconWindow.ClearWindow();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            saveImage();
        }

        private void comboBox_SelectionChanged(object sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox)
            {
                return;
            }

            changeCamera((ECamera)comboBox.SelectedItem);
        }

        private void zoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            FitImage();
        }

        private void zoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomImage(-20);
        }

        private void zoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomImage(20);
        }

        private void dropdownButton_Click(object sender, RoutedEventArgs e)
        {
            this.DropdownPopup.IsOpen = !this.DropdownPopup.IsOpen;
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is not string selectedItem)
            {
                return;
            }

            FrameProcessor = this.frameProcessors[selectedItem];
            StartLive();

            this.DropdownPopup.IsOpen = false;
        }
    }
}