using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.UiUpdaters;

namespace GVisionWpf.UIs.ViewModels
{
    public abstract class PickerDeviceViewViewModel : DeviceViewViewModel
    {
        private const int N_PICKERS = 8;

        protected PickerDeviceViewViewModel()
        {
            BlockLayout = new TableLayout(0, 0);
            FovLayout = new TableLayout(1, 1);
            VisionTableLayout = new TableLayout(1, N_PICKERS);

            InitializeResults();
        }

        protected override void OpenTeachingWindow(List<InspectionResult>? results)
        {
            // ignored
        }

        protected override EColor GetColorOfResult(InspectionResult result)
        {
            return DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType switch
            {
                EInspection.Mark => GlobalSetting.Instance.Inspection.MapColors[result.ErrorType()],
                EInspection.Bga => GlobalSetting.Instance.Inspection.BgaColors[result.ErrorType()],
                EInspection.Qfn => GlobalSetting.Instance.Inspection.QfnColors[result.ErrorType()],
                EInspection.Lga => GlobalSetting.Instance.Inspection.LgaColors[result.ErrorType()],
                _ => EColor.White,
            };
        }

        public override void UpdateResult(List<RenderableInspectionResult> results, int xPosition, int yPosition = 0)
        {

            ReleaseResult(xPosition, yPosition);
            this.ResultsInShots[yPosition][xPosition] = results;

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (RenderableInspectionResult result in results)
                {
                    int blockIdx = CalculateBlockIndex(xPosition, yPosition);
                    int cellIdx = CalculateCellIndex(xPosition, yPosition);

                    Blocks[blockIdx].Cells[cellIdx].CellColor = GetColorOfResult(result.InspectionResult);
                }

                OnPropertyChanged(nameof(Blocks));
            });
        }

        public override void DisplayResult(List<RenderableInspectionResult>? results)
        {
            if (results == null)
            {
                ClearWindow();
                return;
            }

            GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.DisplayInspectionResult, results.First());
        }

        public override void ClearWindow()
        {
            GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.ClearVisionWindow);
        }
    }
}