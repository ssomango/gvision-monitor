namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// CameraWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CameraWindow : FloatingWindowBase
    {
        public CameraWindow(string windowName, ECamera camera) : base(windowName, true)
        {
            InitializeComponent();
            this.xCameraPanel.SourceCameraType = camera;
        }

        public void StopLive()
        {
            this.xCameraPanel.StopLive();
        }
    }
}