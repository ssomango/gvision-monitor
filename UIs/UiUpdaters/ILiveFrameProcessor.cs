using System.Windows;

namespace GVisionWpf.UIs.UiUpdaters
{
    public abstract class ILiveFrameProcessor
    {
        protected ECamera CameraType;
        protected HSmartWindowControlWPF HSmartWindowControlWpf;

        public abstract void Display(HObject image);
        public abstract void SetCameraType(ECamera type);

        protected void DisplayObject(HObject image)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (this.HSmartWindowControlWpf.HalconWindow == IntPtr.Zero)
                {
                    return;
                }

                this.HSmartWindowControlWpf.HalconWindow.DispObj(image);
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}