using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using GVisionWpf.Repositories.Calibrations;
using GVisionWpf.PresentationLayer.Controllers;
using GVisionWpf.Controllers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GVisionWpf.UIs.ViewModels.Calibrations
{
    public sealed partial class PadPitchCalibrationTabViewModel : CalibrationTabViewModel
    {
        private const int PAD_PITCH = 11;

        private readonly PadPitchCalibrationRepository repository = PadPitchCalibrationRepository.Instance;

        public PadPitchCalibrationTabViewModel() : base(ECalibration.PadPitch, "Pad Pitch")
        {
           
        }

        protected override void GetTeaching() => Teaching = this.repository.GetRecipe();

        protected override void SaveRecipe() => repository.SaveRecipe((PadPitchCalibrationTeaching)Teaching);


        public override async Task ExecuteCalibration()
        {
            uint cameraId = FindCameraId(ECamera.PRS);

            PrsCalibrationRequest request = new PrsCalibrationRequest
            {
                CommonBody = new CommonBody
                {
                    Prefix = 0xFFFFFFFF,
                    DataLength = 0x3001,
                    CommonHeader = 0x01000100,
                    CameraId = cameraId,
                    InspectionType = PAD_PITCH
                },
                InspectionResult = 0,
                ErrorType = 0,
                PrsBodies = new List<EachPrsBody>
                {
                    new EachPrsBody()
                }
            };

            CalibrationController.Instance.CalculatePickerOffset(request);
        }
    }
}