using GVisionWpf.Exceptions;
using System.Threading.Tasks;

namespace GVisionWpf.Cameras.CameraTypes
{
    public class EuresysCamera : Camera
    {
        private const string CAM_FILE_ROOT = "Assets/Camfiles/";
        private readonly int device;
        private readonly string mpf;
        private readonly string freeRunningCamFileName;
        private readonly string hardWareCamFileName;
        private readonly string softWareCamFileName;

        private readonly int timeout;

        public EuresysCamera(ECamera cameraType, double pixelPerMillimeter, int device, string freeRunningCamFileName,
            string softWareCamFileName, string hardWareCamFileName, string mpf, bool isHorizontalFlip, bool isVerticalFlip, int maxDelay, int timeout) : base(cameraType, pixelPerMillimeter, isHorizontalFlip, isVerticalFlip)
        {
            this.device = device;
            this.freeRunningCamFileName = freeRunningCamFileName;
            this.hardWareCamFileName = hardWareCamFileName;
            this.softWareCamFileName = softWareCamFileName;
            this.mpf = mpf;
            this.timeout = timeout;
        }


        /*
         * The MaxDelay parameter is obsolete and does not effect the new asynchronous grab.
         * Note that you can check for a too old image by using the MaxDelay parameter of the operator grab_image_async or grab_data_async, respectively.
         * REF: https://www.mvtec.com/doc/halcon/13/en/grab_image_start.html#MaxDelay
         */

        protected override void SetCameraModeConcrete(ECameraMode cameraMode)
        {
            this.CameraMode = cameraMode;

            switch (cameraMode)
            {
                //Json-based setting
                case ECameraMode.FreeRunning:
                    openFrameGrabber(this.freeRunningCamFileName);
                    HOperatorSet.GrabImageStart(this.FrameGrabberHandle, -1);

                    break;

                //Json-based setting
                case ECameraMode.HardwareTrigger:
                    openFrameGrabber(this.hardWareCamFileName);

                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "continuous_grabbing", "enable");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "external_trigger", "true");
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "grab_timeout", this.timeout);
                    HOperatorSet.GrabImageStart(this.FrameGrabberHandle, -1);

                    break;
                default:
                    throw new NotSupportedCameraModeException();
            }
        }

        public override void TriggerShot()
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(75);
                    HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "do_force_trigger", "true");
                }
                catch
                {
                    // ignored
                }

            });

            return;
        }

        public override void AbortGrab()
        {
            HOperatorSet.SetFramegrabberParam(this.FrameGrabberHandle, "do_abort_grab", 1);
        }

        private void openFrameGrabber(string camFileName)
        {
            string camFilePath = this.mpf + ":" + CAM_FILE_ROOT + camFileName;
            HOperatorSet.OpenFramegrabber("MultiCam", 1, 1, 0, 0, 0, 0, "interlaced", 8, "default", -1, "false",
                camFilePath, this.device.ToString(), 1, -1, out HTuple frameGrabberHandle);
            this.FrameGrabberHandle = frameGrabberHandle;
        }
    }
}