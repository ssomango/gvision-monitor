using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.Frames.Windows.Teaching;

namespace GVisionWpf.UIs.ViewModels
{
    public  partial class MapDeviceViewViewModel : DeviceViewViewModel
    {
        private static readonly Lazy<MapDeviceViewViewModel> lazy = new Lazy<MapDeviceViewViewModel>(() => new MapDeviceViewViewModel());
        public static MapDeviceViewViewModel Instance => lazy.Value;

        private MapDeviceViewViewModel()
        {
            Device currentDevice = DeviceRecipeRepository.Instance.GetRecipe();
            BlockLayout = currentDevice.BlockSize;
            FovLayout = currentDevice.FovSize;
            VisionTableLayout = currentDevice.TraySize;

            GVisionMessenger.Instance.Register(this);
            InitializeResults();
        }

        protected override void OpenTeachingWindow(List<InspectionResult>? results)
        {
            if (results == null)
                return;

            ObservableCollection<HObject> shots = new ObservableCollection<HObject>(results.First().Shots);

            _ = DeviceRecipeRepository.Instance.GetRecipe().MapPackageType switch
            {
                EInspection.Mark => new GridMoldTeachingWindow(shots).ShowDialog(),
                EInspection.Bga => new GridBgaTeachingWindow(shots).ShowDialog(),
                EInspection.Qfn => new GridQfnTeachingWindow(shots).ShowDialog(),
                EInspection.Lga => new GridLgaTeachingWindow(shots).ShowDialog(),
                _ => throw new NotImplementedException($"Teaching window for {DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType} is not implemented.")
            };
          
        }

        protected override EColor GetColorOfResult(InspectionResult result)
        {

            return DeviceRecipeRepository.Instance.GetRecipe().MapPackageType switch
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

            GVisionMessenger.Instance.UI.SendMappingUIUpdate(EMoldInspectionUIUpdateType.DisplayInspectionResult, results);
        }

        public override void ClearWindow()
        {
            GVisionMessenger.Instance.UI.SendMappingUIUpdate(EMoldInspectionUIUpdateType.ClearVisionWindow);
        }
    }

    partial class MapDeviceViewViewModel : IRecipient<MoldInspectionUIUpdateMessage>
    {
        public void Receive(MoldInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EMoldInspectionUIUpdateType.AddInspectionResult:
                    UpdateResult(message.RenderableResults, message.XPosition, message.YPosition);
                    break;

                case EMoldInspectionUIUpdateType.ClearAllResults:
                    Dispose();
                    ClearResults();
                    break;

                default:
                    break;
            }
        }
    }
}