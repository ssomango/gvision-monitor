using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Controllers;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using GVisionWpf.PresentationLayer.Controllers;
using GVisionWpf.Repositories.Calibrations;
using GVisionWpf.UIs.Frames.Panels;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GVisionWpf.UIs.ViewModels.Calibrations
{
    public partial class BottomJigCalibrationTabViewModel : CalibrationTabViewModel
    {
        private const int BOTTOM_ZIG = 12;

        private readonly BottomJigCalibrationRepository repository = BottomJigCalibrationRepository.Instance;

        public BottomJigCalibrationTabViewModel() : base(ECalibration.BottomJig, "Bottom Jig")
        {
            
        }

        protected override void GetTeaching() => Teaching = this.repository.GetRecipe();

        protected override void SaveRecipe() => repository.SaveRecipe((BottomJigCalibrationTeaching)Teaching);

        public override async Task ExecuteCalibration()
        {
            uint cameraId = FindCameraId(ECamera.PRS);

            PrsCalibrationRequest request = new PrsCalibrationRequest
            {
                CommonBody = new CommonBody
                {
                    Prefix = 0xffffffff,
                    DataLength = 0x3001,
                    CommonHeader = 0x01000100,
                    CameraId = cameraId,
                    InspectionType = BOTTOM_ZIG
                },
                InspectionResult = 0,
                ErrorType = 0,
                PrsBodies = new List<EachPrsBody>
                {
                    new EachPrsBody
                    {
                        StripBarcode = 0,
                        Sequence = 0,
                        GridTableNumber = 0,
                        X1Orx2 = 0,
                        ZAxisNum = 0,
                        HasDevice = 0,
                        XPickPosition = 0,
                        YPickPosition = 0
                    }
                }
            };

            CalibrationController.Instance.CalculateBottomJigOffset(request);
        }
    }
}