using GVisionWpf.Visions;

namespace GVisionWpf.UIs.UiUpdaters
{
    public class LiveHistogramProcessor : ILiveFrameProcessor
    {
        public LiveHistogramProcessor(ECamera cameraType, HSmartWindowControlWPF hSmartWindowControlWpf)
        {
            this.CameraType = cameraType;
            this.HSmartWindowControlWpf = hSmartWindowControlWpf;
        }

        public override void Display(HObject image)
        {
            VisionOperation.GenGrayHistogram(image, out HObject histogram);
            this.HSmartWindowControlWpf.HalconWindow.SetColor("green");
            this.HSmartWindowControlWpf.HalconWindow.SetDraw("fill");

            this.DisplayObject(image);
            this.DisplayObject(histogram);
            histogram.Dispose();
        }

        public override void SetCameraType(ECamera type)
        {
            this.CameraType = type;
        }
    }
}