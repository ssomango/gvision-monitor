using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.Frames.Windows.Teaching;
using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class PrsDeviceViewViewModel : DeviceViewViewModel
    {
        private static readonly Lazy<PrsDeviceViewViewModel> lazy = new Lazy<PrsDeviceViewViewModel>(() => new PrsDeviceViewViewModel());
        public static PrsDeviceViewViewModel Instance => lazy.Value;

        public PrsDeviceViewViewModel()
        {
            Device currentDevice = DeviceRecipeRepository.Instance.GetRecipe();
            BlockLayout = currentDevice.BlockSize;
            FovLayout = new TableLayout(1, 1);
            VisionTableLayout = currentDevice.TraySize;

            GVisionMessenger.Instance.Register(this);
            InitializeResults();
        }

        protected override void OpenTeachingWindow(List<InspectionResult>? results)
        {
            if (results == null) return;

            HOperatorSet.CopyImage(results.First().Image!, out HObject copiedImage);

            _ = DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType switch
            {
                EInspection.Mark => new MoldTeachingWindow { TeachingImage = copiedImage }.ShowDialog(),
                EInspection.Bga => new BgaTeachingWindow { TeachingImage = copiedImage }.ShowDialog(),
                EInspection.Qfn => new QfnTeachingWindow { TeachingImage = copiedImage }.ShowDialog(),
                EInspection.Lga => new LgaTeachingWindow { TeachingImage = copiedImage }.ShowDialog(),
                _ => throw new NotImplementedException($"Teaching window for {DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType} is not implemented.")
            };
        }

        protected override EColor GetColorOfResult(InspectionResult result)
        {
            return DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType switch
            {
                EInspection.Mark => GlobalSetting.Instance.Inspection.MapColors[result.ErrorType()],
                EInspection.Bga => GlobalSetting.Instance.Inspection.BgaColors[result.ErrorType()],
                EInspection.Lga => GlobalSetting.Instance.Inspection.LgaColors[result.ErrorType()],
                EInspection.Qfn => GlobalSetting.Instance.Inspection.QfnColors[result.ErrorType()],
                _ => EColor.White,
            };
        }

        public override void DisplayResult(List<RenderableInspectionResult>? results)
        {
            if (results == null) return;

            GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.DisplayInspectionResult, results.First());
        }

        public override void ClearWindow()
        {
            GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.ClearVisionWindow);
        }
    }

    partial class PrsDeviceViewViewModel : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.AddInspectionResult:
                    if (message.RenderableResult != null)
                        UpdateResult([message.RenderableResult], message.XPositionForShot, message.YPositionForShot);
                    break;

                case EPrsInspectionUIUpdateType.ClearAllResults:
                    Dispose();
                    ClearResults();
                    break;

                default:
                    break;
            }
        }
    }
}