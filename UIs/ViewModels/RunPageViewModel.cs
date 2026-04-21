using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.UIs.Frames.Windows;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class RunPageViewModel
    {

        public InspectionWindow? MapWindow;
        public InspectionWindow? PrsWindow;
        public InspectionWindow? TopStripWindow;
        public InspectionWindow? BottomStripWindow;

        public RunPageViewModel()
        {
            GVisionMessenger.Instance.RegisterAll(this);
        }
    }

    partial class RunPageViewModel : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            if (PrsWindow is null)
                return;

            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.DisplayInspectionResult:
                case EPrsInspectionUIUpdateType.AddInspectionResult:
                    if (message.RenderableResult is null) 
                        return;

                    PrsWindow.DisplayResult(message.RenderableResult);
                    break;

                case EPrsInspectionUIUpdateType.ClearVisionWindow:
                    PrsWindow.ClearWindow();
                    break;

                default:
                    break;
            }
        }
    }

    partial class RunPageViewModel : IRecipient<MoldInspectionUIUpdateMessage>
    {
        public void Receive(MoldInspectionUIUpdateMessage message)
        {
            if (MapWindow is null)
                return;

            switch (message.UpdateType)
            {
                case EMoldInspectionUIUpdateType.DisplayInspectionResult:
                case EMoldInspectionUIUpdateType.AddInspectionResult:
                    MapWindow.DisplayResult(message.RenderableResults);
                    break;

                case EMoldInspectionUIUpdateType.ClearVisionWindow:
                    MapWindow.ClearWindow();
                    break;

                default:
                    break;
            }
        }
    }

    partial class RunPageViewModel : IRecipient<StripInspectionUIUpdateMessage>
    {
        public void Receive(StripInspectionUIUpdateMessage message)
        {
            switch (message.InspectionType)
            {
                case ERequestInspectionType.TOP_BARCODE:
                    if (message.UpdateType == EStripInspectionUIUpdateType.AddInspectionResult)
                        TopStripWindow?.DisplayResult(message.RenderableResult);
                    break;

                case ERequestInspectionType.BOTTOM_BARCODE:
                    if (message.UpdateType == EStripInspectionUIUpdateType.AddInspectionResult)
                        BottomStripWindow?.DisplayResult(message.RenderableResult);
                    break;

                default:
                    break;
            }
        }
    }
}
