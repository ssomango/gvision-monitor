using GVisionWpf.Cameras;
using GVisionWpf.Visions;

namespace GVisionWpf.UIs.UiUpdaters
{
    public class LiveReticleProcessor : ILiveFrameProcessor
    {
        public LiveReticleProcessor(ECamera cameraType, HSmartWindowControlWPF hSmartWindowControlWpf)
        {
            this.CameraType = cameraType;
            this.HSmartWindowControlWpf = hSmartWindowControlWpf;
        }

        public override void Display(HObject image)
        {
            this.HSmartWindowControlWpf.HalconWindow.SetColor("green");

            HObject reticle = GenReticle(image);

            this.DisplayObject(image);
            this.DisplayObject(reticle);

            reticle.Dispose();
        }

        public HObject GenReticle(HObject image)
        {
            VisionOperation.GetImageMidPoint(image, out Point midPoint);
            double size = CameraManager.Instance.Cameras[this.CameraType].PixelPerMillimeter * 1000;
            VisionOperation.GenReticle(midPoint, size + 25, out HObject reticle);
            return reticle;
        }

        public override void SetCameraType(ECamera cameraType)
        {
            this.CameraType = cameraType;
        }
    }
}