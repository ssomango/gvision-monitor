using GVisionWpf.Visions;

namespace GVisionWpf.UIs.UiUpdaters
{
    public class LiveFullSizeReticleProcessor : ILiveFrameProcessor
    {
        public LiveFullSizeReticleProcessor(ECamera cameraType, HSmartWindowControlWPF hSmartWindowControlWpf)
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
            VisionOperation.GetHObjectSize(image, out double width, out double height, out _);

            const int margin = 3;
            HOperatorSet.GenContourPolygonXld(out HObject verticalLine, new HTuple(height / 2, height / 2), new HTuple(margin, width - margin));
            HOperatorSet.GenContourPolygonXld(out HObject horizontalLine, new HTuple(margin, height - margin), new HTuple(width / 2, width / 2));
            HOperatorSet.ConcatObj(verticalLine, horizontalLine, out HObject reticle);

            return reticle;
        }

        public override void SetCameraType(ECamera type)
        {
            this.CameraType = type;
        }
    }
}