using GVisionWpf.Exceptions;

namespace GVisionWpf.Cameras.CameraTypes
{
    public sealed class FileCamera : Camera
    {
        private readonly string folderPath;

        public FileCamera(ECamera cameraType, double pixelPerMillimeter, bool isHorizontalFlip, bool isVerticalFlip) : base(cameraType, pixelPerMillimeter, isHorizontalFlip, isVerticalFlip)
        {
            this.folderPath = "Assets/SampleImages/" + cameraType;
            this.IsHorizontalFlip = isHorizontalFlip;
            this.IsVerticalFlip = isVerticalFlip;
        }

        protected override void SetCameraModeConcrete(ECameraMode cameraMode)
        {
            this.CameraMode = cameraMode;
            openFrameGrabber();
        }

        public override void TriggerShot()
        {
            // 아무것도 하지 않는게 옳바른 일을 한것
        }

        public override void AbortGrab()
        {
            return;
        }

        private void openFrameGrabber()
        {
            HOperatorSet.OpenFramegrabber("File", 1, 1, 0, 0, 0, 0, "default", 8,
                "default", -1, "false", this.folderPath, "default", -1, -1, out HTuple frameGrabberHandle);
            this.FrameGrabberHandle = frameGrabberHandle;
        }
    }
}