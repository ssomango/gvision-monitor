using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class X2PickerDeviceViewViewModel : PickerDeviceViewViewModel
    {
        private static readonly Lazy<X2PickerDeviceViewViewModel> lazy = new Lazy<X2PickerDeviceViewViewModel>(() => new X2PickerDeviceViewViewModel());
        public static X2PickerDeviceViewViewModel Instance => lazy.Value;

        private X2PickerDeviceViewViewModel()
        { 
            GVisionMessenger.Instance.Register(this);
        }
    }

    partial class X2PickerDeviceViewViewModel : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.UpdatePickerResult:
                    if (message is { PrsBody: { X1Orx2: 1 }, PickerNo: int pickerNo, RenderableResult: not null })
                        UpdateResult([message.RenderableResult], pickerNo, 0);
                    break;

                case EPrsInspectionUIUpdateType.ClearPickerResult:
                case EPrsInspectionUIUpdateType.ClearAllResults:
                    if (message is { PrsBody: { X1Orx2 : 1} })
                    {
                        Dispose();
                        ClearResults();
                    }
                    break;

                default:
                    break;
            }
        }
    }
}