using GVisionWpf.UIs.Frames.Windows;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Pages
{
    /// <summary>
    /// SetupPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SetupPage : Page
    {
        private readonly List<FloatingWindowBase> floatingWindows = new List<FloatingWindowBase>(16);

        private static bool isThisPageInitialized = false;

        public SetupPage()
        {
            InitializeComponent();
            Loaded += loaded;
            Unloaded += unloaded;
        }
        // AML에서 추가
        public IReadOnlyList<FloatingWindowBase> FloatingWindows => floatingWindows;

        private void initializeFloatingWindows()
        {
            this.floatingWindows.Add(new CameraWindow("Camera 1", ECamera.BarCode));
            this.floatingWindows.Add(new CameraWindow("Camera 2", ECamera.NotSelected));
            this.floatingWindows.Add(new CameraWindow("Camera 3", ECamera.PRS));
            this.floatingWindows.Add(new CameraWindow("Camera 4", ECamera.SettingX1));
            this.floatingWindows.Add(new CameraWindow("Camera 5", ECamera.Mapping));
            this.floatingWindows.Add(new CameraWindow("Camera 6", ECamera.SettingX2));
        }

        private void loaded(object sender, RoutedEventArgs e)
        {
            if (!isThisPageInitialized)
            {
                isThisPageInitialized = true;
                initializeFloatingWindows();
            }

            foreach (FloatingWindowBase window in this.floatingWindows)
            {
                window.Show();
            }
        }

        private void unloaded(object sender, RoutedEventArgs e)
        {
            foreach (FloatingWindowBase window in this.floatingWindows)
            {
                window.Hide();
            }
        }
    }
}