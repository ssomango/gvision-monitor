using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using GVisionWpf.Repositories.Calibrations;
using System.Threading.Tasks;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.PresentationLayer.Controllers;
using GVisionWpf.Controllers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GVisionWpf.UIs.ViewModels.Calibrations
{
    public partial class SettingJigCalibrationTabViewModel : CalibrationTabViewModel
    {
        private const int X1_ZIG = 20; // router 값과 동일
        private const int X2_ZIG = 30;

        private readonly SettingJigCalibrationRepository repository = SettingJigCalibrationRepository.Instance;

        public SettingJigCalibrationTabViewModel() : base(ECalibration.SettingJig, "Setting Jig")
        {
           
        }

        protected override void GetTeaching() => Teaching = repository.GetRecipe();

        protected override void SaveRecipe() => repository.SaveRecipe((SettingJigCalibrationTeaching)Teaching);

        public override async Task ExecuteCalibration()
        {
            ECamera cameraType = selectCameraType();

            uint cameraId = FindCameraId(cameraType);
            uint inspectionType = cameraType switch
            {
                ECamera.SettingX1 => X1_ZIG,
                ECamera.SettingX2 => X2_ZIG,
                _ => throw new ArgumentOutOfRangeException()
            };
            uint x1OrX2 = cameraType switch
            {
                ECamera.SettingX1 => 0,
                ECamera.SettingX2 => 1,
                _ => throw new ArgumentOutOfRangeException()
            };

            SettingCalibrationRequest request = new SettingCalibrationRequest
            {
                CommonBody = new CommonBody
                {
                    Prefix = 0xFFFFFFFF,
                    DataLength = 0x3001,
                    CommonHeader = 0x01000100,
                    CameraId = cameraId,
                    InspectionType = inspectionType
                },
                Idk = 0,
                CaptureDone = 0,
                X1orX2 = x1OrX2
            };

            await CalibrationController.Instance.CalculateSettingJigOffset(request);
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