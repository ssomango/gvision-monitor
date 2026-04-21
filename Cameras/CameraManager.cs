using GVisionWpf.Cameras.CamearaQueues;
using GVisionWpf.Cameras.CameraTypes;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.Services;
using GVisionWpf.UIs.ViewModels;
using GVisionWpf.Visions;
using log4net;
using System.IO;
using System.Threading.Tasks;

namespace GVisionWpf.Cameras
{
    public sealed class CameraManager
    {
        private static readonly Lazy<CameraManager> lazy = new Lazy<CameraManager>(() => new CameraManager());
        public static CameraManager Instance => lazy.Value;

        private readonly IlluminationService illuminationService;

        public ImageQueue PrsQueue;
        public ImageQueue MappingQueue;

        public Dictionary<ECamera, Camera> Cameras;
        public Dictionary<ECamera, Camera> RealCameras = new Dictionary<ECamera, Camera>();
        public Dictionary<ECamera, Camera> FileCameras = new Dictionary<ECamera, Camera>();

        private static readonly ILog log = LogManager.GetLogger("Camera");

        private CameraManager()
        {
            this.illuminationService = IlluminationService.Instance;
        }

        ~CameraManager()
        {
            ReleaseAllFrameGrabber();
        }

        public void InitializeAllCamera()
        {
            // public Dictionary<ECamera, Camera> Cameras 에 필요한 카메라를 시스템 시작할 때 로드
            // 일단 시작은 live 모드로
            log.Info("Starting camera load.");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Starting camera load.");


            this.RealCameras = CameraLoader.LoadCameraSettings();
            this.FileCameras = CameraLoader.LoadAlternativeFileCameras();

            try
            {
                this.Cameras = this.RealCameras;
                SetLiveMode();
            }
            catch
            {
                this.RealCameras.Clear();
                this.RealCameras = this.FileCameras;

                this.Cameras = this.FileCameras;
                SetLiveMode();

                log.Info("An unsupported camera was detected, so a file camera has been loaded.");
                GVisionMessenger.Instance.UI.SendSystemInfoMessage("An unsupported camera was detected, so a file camera has been loaded.");
            }

            log.Info("Camera load is complete.");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Camera load is complete.");
        }

        public void SetRunMode()
        {
            ResetTriggerListener();

            log.Info("All cameras are entering Run mode.");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("All cameras are entering Run mode.");

            ReleaseAllFrameGrabber();

            //각 카메라에 대한 정보를 가지고 런모드 셋팅 
            foreach (var cameraSetting in GlobalSetting.Instance.CameraInfos)
            {
                this.Cameras[cameraSetting.CameraType].SetCameraMode(cameraSetting.RunMode);

                if (cameraSetting.LiveMode == ECameraMode.HardwareTrigger)
                {
                    HOperatorSet.SetFramegrabberParam(this.Cameras[cameraSetting.CameraType].FrameGrabberHandle, "grab_timeout", -1);
                }
            }

            if (RealCameras[ECamera.PRS].cameraTriggerMode != ECameraTriggerMode.SoftwareTrigger && RealCameras[ECamera.PRS] is not FileCamera)
            {
                PrsQueue = PrsImageQueue.Instance;
                this.RealCameras[ECamera.PRS].AddTriggerObserver(this.PrsQueue);
                this.RealCameras[ECamera.PRS].StartListeningPrsTrigger();
            }

            if (RealCameras[ECamera.Mapping].cameraTriggerMode != ECameraTriggerMode.SoftwareTrigger && RealCameras[ECamera.Mapping] is not FileCamera)
            {
                MappingQueue = MappingImageQueue.Instance;
                this.RealCameras[ECamera.Mapping].AddTriggerObserver(this.MappingQueue);
                this.RealCameras[ECamera.Mapping].StartListeningMapTrigger();
            }
            else if (FileCameras[ECamera.Mapping] is FileCamera)
            {
                MappingQueue = LocalMappingImageQueue.Instance;
                this.FileCameras[ECamera.Mapping].AddTriggerObserver(this.MappingQueue);
                this.FileCameras[ECamera.Mapping].StartListeningMapTrigger();
            }
        }


        public void SetLiveMode()
        {
            log.Info("All cameras are entering Setup mode.");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("All cameras are entering Setup mode.");
            //AbortGrabAllFrameGrabber();

            ResetTriggerListener();
            ReleaseAllFrameGrabber();

            foreach (var cameraSetting in GlobalSetting.Instance.CameraInfos)
            {
                this.Cameras[cameraSetting.CameraType].SetCameraMode(cameraSetting.LiveMode);

                if (cameraSetting.LiveMode == ECameraMode.HardwareTrigger)
                {
                    HOperatorSet.SetFramegrabberParam(this.Cameras[cameraSetting.CameraType].FrameGrabberHandle, "grab_timeout", -1);
                }
            }
        }

        public void SetRealCameraMode()
        {
            this.Cameras = this.RealCameras;
        }

        public void SetFileCameraMode()
        {
            this.Cameras = this.FileCameras;
        }


        //------------------------------------------- 다른 스크립트로 가자 이미지 취득 방식에 따른 메소드들----------------------------------------------------------//


        public HObject RetrieveImage(ECamera cameraType)
        {
            HObject image = this.Cameras![cameraType].RetrieveImage();
            image = this.preProcessImage(image, cameraType);

            return image;
        }


        public HObject RetrieveImageSync(ECamera cameraType)
        {
            HObject image = this.Cameras![cameraType].RetrieveImageSync();
            image = this.preProcessImage(image, cameraType);

            return image;
        }

        public List<HObject> RetrieveMultiShots(ECamera cameraType)
        {
            IlluminationRecipe illuminationRecipe = this.illuminationService.GetIlluminationRecipe();
            var shotsRecipe = illuminationRecipe.Setting[cameraType];

            List<HObject> images = new List<HObject>();

            foreach (var shotRecipe in shotsRecipe)
            {

                LightManager.Instance.SetBrightness(cameraType, shotRecipe);

                Task.Delay(10);

                CameraManager.Instance.Cameras![cameraType].TriggerShot();

                HObject image;

                if (CameraManager.Instance.Cameras[cameraType] is FileCamera)
                {
                    image = CameraManager.Instance.RetrieveImageSync(cameraType);
                }
                else
                {
                    switch (cameraType)
                    {
                        case ECamera.Mapping:
                            image = CameraManager.Instance.MappingQueue.Dequeue(500);
                            break;
                        case ECamera.PRS:
                            image = CameraManager.Instance.PrsQueue.Dequeue(500);
                            break;
                        default:
                            image = CameraManager.Instance.RetrieveImageSync(cameraType);
                            break;
                    }
                }

                images.Add(image);
            }

            LightManager.Instance.TurnOffAllLights(cameraType);

            return images;
        }


        public async Task<HObject> RetrieveImageWithIlluminationSlow(ECamera cameraType)
        {
            LightManager.Instance.TurnOnLight(cameraType);
            await Task.Delay(200);

            CameraManager.Instance.Cameras![cameraType].TriggerShot();
            HObject image = CameraManager.Instance.RetrieveImageSync(cameraType);

            LightManager.Instance.TurnOffAllLights(cameraType);

            return image;
        }

        public HObject RetrieveTriggeredImage(ECamera cameraType)
        {
            try
            {
                HObject image = this.RetrieveImage(cameraType);
                return image;
            }
            catch
            {
                throw new CameraTriggerException();
            }
        }

        //------------------------------------------- 다른 스크립트로 가자 잡다구리한 메소드들----------------------------------------------------------//

        public HObject preProcessImage(HObject image, ECamera cameraType)
        {
            //현재는 크롭만
            bool hasCameraDomainSetting = GlobalSetting.Instance.CameraDomain.TryGetValue(cameraType, out Resolution? resolution);

            if (hasCameraDomainSetting && resolution != null)
            {
                VisionOperation.CropImage(image, resolution.Width, resolution.Height, out image);
            }

            switch (GlobalSetting.Instance.SystemType)
            {
                case ESystemType.HanaMicron:
                    if (cameraType == ECamera.BarCode)
                    {
                        /// 이미지 크기 확인
                        HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);

                        // 비율 설정 (예: 상단 80%만 남기고 하단 20% 잘라냄)

                        /* Reverse Loading 시, Y Offset 값이 음수가 검출되어야 함.
                         * 1,2,3호기 ratio 0.8, Crop 방향 아래쪽
                         * 4호기 ratio 0.8, Crop 방향 위쪽
                         * 5호기 Ratio : 0.9, Crop 방향 아래쪽
                        */

                        double ratio = 0.8;
                        HTuple cropHeight = height * ratio;   // 남길 높이
                        HTuple row = 0; // Crop 방향 아래쪽
                        HTuple startRow = height * (1 - ratio); // Crop 방향 위쪽

                        // Crop 수행: 상단부터 cropHeight 만큼 남김
                        HOperatorSet.CropPart(image, out image, row, 0, width, cropHeight);
                    }
                    break;
            }

            return image;
        }

        public void ReleaseAllFrameGrabber()
        {
            foreach (Camera camera in this.Cameras.Values)
            {
                camera.ReleaseFrameGrabber();
            }
        }

        public void StopAllLiveSource()
        {
            foreach (Camera camera in this.Cameras.Values)
            {
                if (camera.CameraMode == ECameraMode.None)
                {
                    continue;
                }

                camera.ClearLiveObservers();

                // Consumes grab_image waiting. aborting doesn't work. but idk why
                if (camera.CameraMode == ECameraMode.HardwareTrigger || camera.CameraMode == ECameraMode.SoftwareTrigger)
                {
                    camera.TriggerShot();
                }
            }
        }

        public void WriteAllImage()
        {
            foreach (ECamera cameraType in this.Cameras.Keys)
            {
                WriteImage(cameraType);
            }
        }

        public void WriteImage(ECamera cameraType)
        {
            HObject? image = this.Cameras![cameraType].PreviousFrameImage;
            if (image == null) { return; }

            string imageFolderPath = $"DB/Images/{DateTime.Now:yyyy-MM-dd}/Captured/{cameraType}";
            if (!Directory.Exists(imageFolderPath))
            {
                Directory.CreateDirectory(imageFolderPath);
            }

            string path = $"{imageFolderPath}/{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
            HOperatorSet.WriteImage(image, "png fastest", 0, path);
        }

        public void ResetTriggerListener()
        {
            this.RealCameras[ECamera.PRS].ClearTriggerObservers();
            this.FileCameras[ECamera.PRS].ClearTriggerObservers();

            this.RealCameras[ECamera.PRS].StopListeningPrsTrigger();
            this.FileCameras[ECamera.PRS].StopListeningPrsTrigger();

            this.RealCameras[ECamera.Mapping].ClearTriggerObservers();
            this.FileCameras[ECamera.Mapping].ClearTriggerObservers();

            this.RealCameras[ECamera.Mapping].StopListeningMappingTrigger();
            this.FileCameras[ECamera.Mapping].StopListeningMappingTrigger();

            if (this.PrsQueue != null) { this.PrsQueue.Clear(); }
            if (this.MappingQueue != null) { this.MappingQueue.Clear(); }

        }
    }
}