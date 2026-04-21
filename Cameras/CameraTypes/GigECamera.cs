using GVisionWpf.Exceptions;

namespace GVisionWpf.Cameras.CameraTypes
{
    public class GigECamera : Camera
    {
        private readonly string device;

        public GigECamera(ECamera cameraType, double pixelPerMillimeter, string device, bool isHorizontalFlip, bool isVerticalFlip) : base(cameraType, pixelPerMillimeter, isHorizontalFlip, isVerticalFlip)
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
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]trigger", "Off");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]exposure_auto", "Off");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]gain_auto", "Off");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]exposure", 20000);
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "grab_timeout", 1000);

                    HOperatorSet.GrabImageStart(this.FrameGrabberHandle, -1);
                    break;

                case ECameraMode.SoftwareTrigger:
                    openFrameGrabber();
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "AcquisitionMode", "Continuous");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]exposure_auto", "Off");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]gain_auto", "Off");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]exposure", 20000);
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]trigger", "Software");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "grab_timeout", 1000);

                    HOperatorSet.GrabImageStart(this.FrameGrabberHandle, -1);
                    break;

                default:
                    throw new NotSupportedCameraModeException();
            }
        }

        public override void TriggerShot()
        {
            /*Task.Run(() =>
            {
                try
                {
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "[Consumer]trigger_software", 1);
                }
                catch (Exception)
                {
                    throw new Exception("Failed to trigger GigE Camera.");
                }
            });*/
        }

        public override void AbortGrab()
        {
            HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "do_abort_grab", 1);
        }

        private void openFrameGrabber()
        {
            HOperatorSet.OpenFramegrabber("GigEVision2", 0, 0, 0, 0, 0, 0, "progressive", -1, "default", -1, "false", "default", this.device, 0, -1, out HTuple frameGrabberHandle);
            this.FrameGrabberHandle = frameGrabberHandle;
        }
    }
}