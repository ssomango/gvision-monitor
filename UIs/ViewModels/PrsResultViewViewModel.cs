using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Repositories;
using log4net.Filter;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class PrsResultViewViewModel : ResultViewViewModel
    {
        private static readonly Lazy<PrsResultViewViewModel> lazy = new Lazy<PrsResultViewViewModel>(() => new PrsResultViewViewModel());
        public static PrsResultViewViewModel Instance => lazy.Value;

        public PrsResultViewViewModel()
        {
            Initialize();
            GVisionMessenger.Instance.Register(this);
        }

        public void Initialize()
        {
            InspectionViewModels.Clear();

            switch (DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType)
            {
                case EInspection.Mark:
                    Initialize(EInspection.Mark, GlobalSetting.Instance.Inspection.MapColors);
                    break;
                case EInspection.Bga:
                    Initialize(EInspection.Bga, GlobalSetting.Instance.Inspection.BgaColors);
                    break;
                case EInspection.Qfn:
                    Initialize(EInspection.Qfn, GlobalSetting.Instance.Inspection.QfnColors);
                    break;
                case EInspection.Lga:
                    Initialize(EInspection.Lga, GlobalSetting.Instance.Inspection.LgaColors);
                    break;
            }
        }
    }

    partial class PrsResultViewViewModel : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.AddInspectionResult:
                    Update([message.RenderableResult.InspectionResult]);
                    break;

                default:
                    break;
            }
        }
    }
}