using GVisionWpf.Exceptions;

namespace GVisionWpf.Cameras.CameraTypes
{
    public class UsbCamera : Camera
    {
        private readonly string device;

        public UsbCamera(ECamera cameraType, double pixelPerMillimeter, string device, bool isHorizontalFlip, bool isVerticalFlip) : base(cameraType, pixelPerMillimeter, isHorizontalFlip, isVerticalFlip)
        {
            this.device = device;
            this.IsHorizontalFlip = isHorizontalFlip;
            this.IsVerticalFlip = isVerticalFlip;
        }

        protected override void SetCameraModeConcrete(ECameraMode cameraMode)
        {
            this.CameraMode = cameraMode;

            switch (cameraMode)
            {
                case ECameraMode.FreeRunning:
                    openFrameGrabber();
                    //HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "exposure", -6);
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "exposure", -9);

                    break;
                default:
                    throw new NotSupportedCameraModeException();
            }
        }

        public override void TriggerShot()
        {
            return;
        }

        public override void AbortGrab()
        {
            return;
        }

        private void openFrameGrabber()
        {
            HOperatorSet.OpenFramegrabber("DirectShow", 0, 0, 0, 0, 0, 0, "default", -1, "gray", -1, "false", "default", this.device, 0, -1, out HTuple frameGrabberHandle);
            this.FrameGrabberHandle = frameGrabberHandle;
        }
    }
}