using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using System.Windows.Input;
using GVisionWpf.Repositories.Calibrations;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.Controllers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GVisionWpf.UIs.ViewModels.Calibrations
{
    public sealed partial class TrayCalibrationTabViewModel : CalibrationTabViewModel
    {
        private const int X1_TRAY_TRANSFER_CAL = 22;
        private const int X2_TRAY_TRANSFER_CAL = 32;

        private readonly TrayCalibrationRepository repository = TrayCalibrationRepository.Instance;

        public TrayCalibrationTabViewModel() : base(ECalibration.Tray, "Tray")
        {
            
        }

        protected override void GetTeaching() => Teaching = this.repository.GetRecipe();

        protected override void SaveRecipe() => repository.SaveRecipe((TrayCalibrationTeaching)Teaching);


        public override async Task ExecuteCalibration()
        {
            ECamera cameraType = selectCameraType();

            uint cameraId = FindCameraId(cameraType);
            uint inspectionType = cameraType switch
            {
                ECamera.SettingX1 => X1_TRAY_TRANSFER_CAL,
                ECamera.SettingX2 => X2_TRAY_TRANSFER_CAL,
                _ => throw new ArgumentOutOfRangeException()
            };
            uint x1OrX2 = cameraType switch
            {
                ECamera.SettingX1 => 0,
                ECamera.SettingX2 => 1,
                _ => throw new ArgumentOutOfRangeException()
            };

            ThreePointCalibrationRequest request = new ThreePointCalibrationRequest
            {
                CommonBody = new CommonBody
                {
                    Prefix = 0xFFFFFFFF,
                    DataLength = 0x3001,
                    CommonHeader = 0x01000100,
                    CameraId = cameraId,
                    InspectionType = inspectionType
                },
                TriggerType = 0,
                CaptureDone = 0,
                X1orX2 = x1OrX2
            };

            await CalibrationController.Instance.CalculateTrayTransferOffset(request);
        }

        private ECamera selectCameraType()
        {
            List<ECamera> cameraList = new List<ECamera> { ECamera.SettingX1, ECamera.SettingX2 };

            SelectCameraWindow window = new SelectCameraWindow(cameraList);
            bool? dialogResult = window.ShowDialog();

            return dialogResult == true ? ECamera.SettingX1 : ECamera.SettingX2;
        }
    }
}