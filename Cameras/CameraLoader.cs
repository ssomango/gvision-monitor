using GVisionWpf.Cameras.CameraTypes;
using GVisionWpf.GlobalStates;

namespace GVisionWpf.Cameras
{
    public class CameraLoader
    {
        //camera.json 정리하는 메소드
        /*
         * new Dictionary<ECamera, Camera>
            {
                { 
                ECamera.NotSelected, new FileCamera(
                ECamera.NotSelected,
                pixelPerMillimeter: 0.1,
                horizontalFlip: false,
                verticalFlip: false) 
                },

                { 
                ECamera.PRS, new EuresysCamera(
                }
            }
         * 
         */
        public static Dictionary<ECamera, Camera> LoadCameraSettings()
        {
            try
            {
                Dictionary<ECamera, Camera> Cameras = new Dictionary<ECamera, Camera>();
                var cameraSettingsList = GlobalSetting.Instance.CameraInfos;

                foreach (var cameraSetting in cameraSettingsList)
                {
                    ECamera cameraEnumType = cameraSetting.CameraType;
                    bool horizontalFlip = cameraSetting.HorizontalFlip;
                    bool verticalFlip = cameraSetting.VerticalFlip;
                    ECameraMode freerunning = cameraSetting.LiveMode;
                    ECameraMode runmode = cameraSetting.RunMode;

                    string? deviceName;
                    switch (cameraSetting.CameraInterface)
                    {
                        case ECameraInterface.File:
                            Cameras[cameraEnumType] = new FileCamera(cameraEnumType, cameraSetting.PixelPerMillimeter, horizontalFlip, verticalFlip);
                            break;

                        case ECameraInterface.Euresys:
                            int device = Convert.ToInt32(cameraSetting.Params["device"]);
                            string? freeRunningCamFile = cameraSetting.Params["freeRunningCamFile"].ToString();
                            string? softWareCamFile = cameraSetting.Params["softWareCamFile"].ToString();
                            string? hardWareCamFile = cameraSetting.Params["hardWareCamFile"].ToString();
                            string? mpf = cameraSetting.Params["mpf"].ToString();

                            string? timeoutInString = cameraSetting.Params["timeout"].ToString();
                            int timeout = timeoutInString != null ? Convert.ToInt32(timeoutInString) : 500;
                            int maxDelay = (int)(cameraSetting.MaxDelay != null ? cameraSetting.MaxDelay! : -1);

                            Cameras[cameraEnumType] = new EuresysCamera(
                                cameraEnumType,
                                cameraSetting.PixelPerMillimeter,
                                device,
                                freeRunningCamFile,
                                softWareCamFile,
                                hardWareCamFile,
                                mpf,
                                horizontalFlip,
                                verticalFlip,
                                maxDelay,
                                timeout);

                            break;

                        case ECameraInterface.Usb:
                            HOperatorSet.InfoFramegrabber("DirectShow", "device", out _, out HTuple devices);
                            string[] directShowDeviceNames = devices.SArr;

                            deviceName = cameraSetting.Params["device"].ToString();

                            foreach (string usbFullDeviceName in directShowDeviceNames)
                            {
                                if (usbFullDeviceName.Contains(deviceName))
                                {
                                    Cameras[cameraEnumType] = new UsbCamera(cameraEnumType, cameraSetting.PixelPerMillimeter, usbFullDeviceName, horizontalFlip, verticalFlip);
                                    break;
                                }
                            }

                            break;

                        case ECameraInterface.GigE:
                            deviceName = cameraSetting.Params["device"].ToString();

                            Cameras[cameraEnumType] = new GigECamera(
                                cameraEnumType,
                                cameraSetting.PixelPerMillimeter,
                                deviceName,
                                horizontalFlip,
                                verticalFlip);
                            break;

                        default:
                            throw new Exception($"Unknown camera type: {cameraSetting.CameraInterface}");
                    }

                    Cameras[cameraEnumType].cameraTriggerMode = cameraSetting.CameraTriggerMode;
                }


                return Cameras;
            }

            catch (Exception ex)
            {
                throw ex;
                //throw new WrongSettingException();
            }
        }

        public static Dictionary<ECamera, Camera> LoadAlternativeFileCameras()
        {
            Dictionary<ECamera, Camera> Cameras = new Dictionary<ECamera, Camera>();
            var cameraSettingsList = GlobalSetting.Instance.CameraInfos;

            foreach (var cameraSetting in cameraSettingsList)
            {
                ECamera cameraEnumType = cameraSetting.CameraType;
                Cameras[cameraEnumType] = new FileCamera(cameraEnumType, cameraSetting.PixelPerMillimeter, false, false);
                Cameras[cameraEnumType].cameraTriggerMode = cameraSetting.CameraTriggerMode;
            }

            return Cameras;
        }
    }
}