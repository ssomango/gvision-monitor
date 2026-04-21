using System.Threading.Tasks;
using GVisionWpf.Cameras;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Dtos.Response;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.PresentationLayer.Controllers;
using GVisionWpf.Services;
using GVisionWpf.UIs.ViewModels;
using GVisionWpf.UIs.ViewModels.Calibrations;
using log4net;

namespace GVisionWpf.Controllers
{
    public class CalibrationController : BaseController
    {
        private static readonly Lazy<CalibrationController> lazy = new Lazy<CalibrationController>(() => new CalibrationController());
        public static CalibrationController Instance => lazy.Value;

        private readonly CalibrationService calibrationService;

        private static readonly ILog log = LogManager.GetLogger("Calibration");

        private CalibrationController()
        {
            this.calibrationService = CalibrationService.Instance;
        }

        public void CalculatePickerOffset(PrsCalibrationRequest calibrationRequest)
        {
            log.Info($"[Request] Picker Calibration ({calibrationRequest})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Request] Picker Calibration");

            const string taskName = "PadPitchCalibration";
            ThrowUnlessAllowedInCurrentMode(taskName);

            CameraManager.Instance.Cameras![ECamera.PRS].TriggerShot();
            HObject image = CameraManager.Instance.RetrieveTriggeredImage(ECamera.PRS);
            ECamera camera = ECamera.PRS;

            CalibrationResult result = this.calibrationService.CalculatePickerOffset(image, camera);
            CalibrationViewModel.Instance.AddResult(result);

            PrsCalibrationResponse calibrationResponse = new PrsCalibrationResponse
            {
                CommonBody = calibrationRequest.CommonBody!,
                InspectionResult = result.IsFound ? 1u : 0u,
                ErrorType = calibrationRequest.ErrorType,
                PrsBody = calibrationRequest.PrsBodies!.First(),
            };

            calibrationResponse.CommonBody.Prefix = 0xffffffff;
            calibrationResponse.CommonBody.DataLength = 72;
            calibrationResponse.CommonBody.CommonHeader = 1;
            // calibrationResponse.CommonBody.CameraId = 1; supposed to be bypassed
            calibrationResponse.ErrorType = 0;

            base.SetOffset(calibrationResponse, result.Offset, taskName);

            base.Respond(calibrationResponse);

            log.Info($"[Response] Picker Calibration ({calibrationResponse})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Response] Picker Calibration X:{calibrationResponse.XOffset}, Y:{calibrationResponse.YOffset}, T:{calibrationResponse.TOffset}");
        }

        public void CalculateBottomJigOffset(PrsCalibrationRequest calibrationRequest)
        {
            log.Info($"[Request] Bottom JIG Calibration ({calibrationRequest})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Request] Bottom JIG Calibration");

            const string taskName = "BottomJigCalibration";
            ThrowUnlessAllowedInCurrentMode(taskName);

            CameraManager.Instance.Cameras![ECamera.PRS].TriggerShot();
            HObject image = CameraManager.Instance.RetrieveTriggeredImage(ECamera.PRS);
            ECamera camera = ECamera.PRS;

            CalibrationResult result = this.calibrationService.CalculateBottomJigOffset(image, camera);
            CalibrationViewModel.Instance.AddResult(result);

            PrsCalibrationResponse calibrationResponse = new PrsCalibrationResponse
            {
                CommonBody = calibrationRequest.CommonBody!,
                InspectionResult = result.IsFound ? 1u : 0u,
                ErrorType = calibrationRequest.ErrorType,
                PrsBody = calibrationRequest.PrsBodies!.First(),
            };

            calibrationResponse.CommonBody.Prefix = 0xffffffff;
            calibrationResponse.CommonBody.DataLength = 72;
            calibrationResponse.CommonBody.CommonHeader = 1;
            // calibrationResponse.CommonBody.CameraId = 1; supposed to be bypassed
            calibrationResponse.ErrorType = 0;

            base.SetOffset(calibrationResponse, result.Offset, taskName);

            base.Respond(calibrationResponse);

            log.Info($"[Response] Bottom JIG Calibration ({calibrationResponse})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Response] Bottom JIG PackageOffset X:{calibrationResponse.XOffset}, Y:{calibrationResponse.YOffset}, T:{calibrationResponse.TOffset}");
        }

        public async Task CalculateSettingJigOffset(SettingCalibrationRequest calibrationRequest)
        {
            log.Info($"[Request] Setting JIG Calibration ({calibrationRequest})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Request] Setting JIG Calibration");

            ECamera camera = GlobalSetting.Instance.ECameraNos[calibrationRequest.CommonBody!.CameraId];
            string taskName = camera switch
            {
                ECamera.SettingX1 => "SettingX1SettingJigCalibration",
                ECamera.SettingX2 => "SettingX2SettingJigCalibration",
                _ => string.Empty
            };
            ThrowUnlessAllowedInCurrentMode(taskName);

            HObject image = await CameraManager.Instance.RetrieveImageWithIlluminationSlow(camera);

            CalibrationResult result = this.calibrationService.CalculateSettingZigOffset(image, camera);
            CalibrationViewModel.Instance.AddResult(result);

            SettingCalibrationResponse calibrationResponse = new SettingCalibrationResponse
            {
                CommonBody = calibrationRequest.CommonBody!,
                InspectionResult = result.IsFound ? 1u : 0u,
                X1orX2 = calibrationRequest.X1orX2,
                ErrorType = 0,
            };

            calibrationResponse.CommonBody.Prefix = 0xffffffff;
            calibrationResponse.CommonBody.DataLength = 44;
            calibrationResponse.CommonBody.CommonHeader = 1;

            base.SetOffset(calibrationResponse, result.Offset, taskName);

            base.Respond(calibrationResponse);

            log.Info($"[Response] Setting JIG Calibration ({calibrationResponse})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Response] Setting JIG Offset X:{calibrationResponse.XOffset}, Y:{calibrationResponse.YOffset}, T:{calibrationResponse.TOffset}");
        }

        public async Task CalculateTrayTransferOffset(ThreePointCalibrationRequest calibrationRequest)
        {
            log.Info($"[Request] TrayTransfer Calibration ({calibrationRequest})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Request] TrayTransfer Calibration");

            ECamera camera = GlobalSetting.Instance.ECameraNos[calibrationRequest.CommonBody!.CameraId];
            string taskName = camera switch
            {
                ECamera.SettingX1 => "SettingX1TrayTransferCalibration",
                ECamera.SettingX2 => "SettingX2TrayTransferCalibration",
                _ => string.Empty
            };
            ThrowUnlessAllowedInCurrentMode(taskName);

            HObject image = await CameraManager.Instance.RetrieveImageWithIlluminationSlow(camera);

            CalibrationResult result = this.calibrationService.CalculateTrayTransferOffset(image, camera);
            CalibrationViewModel.Instance.AddResult(result);

            ThreePointCalibrationResponse calibrationResponse = new ThreePointCalibrationResponse
            {
                CommonBody = calibrationRequest.CommonBody!,
                InspectionResult = result.IsFound ? 1u : 0u,
                X1orX2 = calibrationRequest.X1orX2,
                ErrorType = 0,
            };
            calibrationResponse.CommonBody.Prefix = 0xffffffff;
            calibrationResponse.CommonBody.DataLength = 44;
            calibrationResponse.CommonBody.CommonHeader = 1;

            base.SetOffset(calibrationResponse, result.Offset, taskName);

            base.Respond(calibrationResponse);

            log.Info($"[Response] TrayTransfer Calibration ({calibrationResponse})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Response] TrayTransfer Calibration X:{calibrationResponse.XOffset}, Y:{calibrationResponse.YOffset}, T:{calibrationResponse.TOffset}");
        }

        public async Task CalculateVisionTableOffset(ThreePointCalibrationRequest calibrationRequest)
        {
            log.Info($"[Request] VisionTable Calibration ({calibrationRequest})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Request] VisionTable Calibration");

            ECamera camera = GlobalSetting.Instance.ECameraNos[calibrationRequest.CommonBody!.CameraId];
            string taskName = camera switch
            {
                ECamera.SettingX1 => "SettingX1VisionTableCalibration",
                ECamera.SettingX2 => "SettingX2VisionTableCalibration",
                _ => string.Empty
            };

            ThrowUnlessAllowedInCurrentMode(taskName);

            HObject image = await CameraManager.Instance.RetrieveImageWithIlluminationSlow(camera);

            CalibrationResult result = this.calibrationService.CalculateVisionTableOffset(image, camera);
            CalibrationViewModel.Instance.AddResult(result);

            ThreePointCalibrationResponse calibrationResponse = new ThreePointCalibrationResponse
            {
                CommonBody = calibrationRequest.CommonBody!,
                InspectionResult = result.IsFound ? 1u : 0u,
                X1orX2 = calibrationRequest.X1orX2,
                ErrorType = 0,
            };
            calibrationResponse.CommonBody.Prefix = 0xffffffff;
            calibrationResponse.CommonBody.DataLength = 44;
            calibrationResponse.CommonBody.CommonHeader = 1;

            base.SetOffset(calibrationResponse, result.Offset, taskName);

            base.Respond(calibrationResponse);

            log.Info($"[Response] VisionTable Calibration ({calibrationResponse})");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Response] VisionTable Calibration X:{calibrationResponse.XOffset}, Y:{calibrationResponse.YOffset}, T:{calibrationResponse.TOffset}");
        }
    }
}