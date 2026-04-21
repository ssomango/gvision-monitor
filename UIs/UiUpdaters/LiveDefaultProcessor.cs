namespace GVisionWpf.UIs.UiUpdaters
{
    public class LiveDefaultProcessor : ILiveFrameProcessor
    {
        public LiveDefaultProcessor(ECamera cameraType, HSmartWindowControlWPF hSmartWindowControlWpf)
        {
            this.CameraType = cameraType;
            this.HSmartWindowControlWpf = hSmartWindowControlWpf;
        }

        public override void Display(HObject image)
        {
            this.DisplayObject(image);
        }

        public override void SetCameraType(ECamera type)
        {
            this.CameraType = type;
        }
    }
}