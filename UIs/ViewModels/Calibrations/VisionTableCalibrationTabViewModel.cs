using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using System.Windows.Input;
using GVisionWpf.Repositories.Calibrations;
using System.Threading.Tasks;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.PresentationLayer.Controllers;
using GVisionWpf.Controllers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GVisionWpf.UIs.ViewModels.Calibrations
{
    public sealed partial class VisionTableCalibrationTabViewModel : CalibrationTabViewModel
    {
        private const int X1_GRID_TABLE_CAL = 21;
        private const int X2_GRID_TABLE_CAL = 31;

        private readonly VisionTableCalibrationRepository repository = VisionTableCalibrationRepository.Instance;


        public VisionTableCalibrationTabViewModel() : base(ECalibration.VisionTable, "Vision Table")
        {
          
        }

        protected override void GetTeaching() => Teaching = this.repository.GetRecipe();

        protected override void SaveRecipe() => repository.SaveRecipe((VisionTableCalibrationTeaching)Teaching);

        public override async Task ExecuteCalibration()
        {
            ECamera cameraType = selectCameraType();

            uint cameraId = FindCameraId(cameraType);
            uint inspectionType = cameraType switch
            {
                ECamera.SettingX1 => X1_GRID_TABLE_CAL,
                ECamera.SettingX2 => X2_GRID_TABLE_CAL,
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

            await CalibrationController.Instance.CalculateVisionTableOffset(request);
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