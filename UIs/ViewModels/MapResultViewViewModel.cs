using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Repositories;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class MapResultViewViewModel : ResultViewViewModel
    {
        private static readonly Lazy<MapResultViewViewModel> lazy = new Lazy<MapResultViewViewModel>(() => new MapResultViewViewModel());
        public static MapResultViewViewModel Instance => lazy.Value;

        private MapResultViewViewModel()
        {
            Initialize();
            GVisionMessenger.Instance.Register(this);
        }

        public void Initialize()
        {
            InspectionViewModels.Clear();

            switch (DeviceRecipeRepository.Instance.GetRecipe().MapPackageType)
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

    partial class MapResultViewViewModel : IRecipient<MoldInspectionUIUpdateMessage>
    {
        public void Receive(MoldInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EMoldInspectionUIUpdateType.AddInspectionResult:
                    Update(message.RenderableResults
                        .Select(r => r.InspectionResult)
                        .ToList());
                    break;

                default:
                    break;
            }
        }
    }
}