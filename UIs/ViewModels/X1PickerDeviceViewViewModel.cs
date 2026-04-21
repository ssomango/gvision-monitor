using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Events.Message.Inspection;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class X1PickerDeviceViewViewModel : PickerDeviceViewViewModel
    {
        private static readonly Lazy<X1PickerDeviceViewViewModel> lazy = new Lazy<X1PickerDeviceViewViewModel>(() => new X1PickerDeviceViewViewModel());
        public static X1PickerDeviceViewViewModel Instance => lazy.Value;

        private X1PickerDeviceViewViewModel() 
        {
            GVisionMessenger.Instance.Register(this);
        }
    }

    partial class X1PickerDeviceViewViewModel : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.UpdatePickerResult:
                    if (message is { PrsBody: { X1Orx2: 0 }, PickerNo: int pickerNo, RenderableResult: not null })
                        UpdateResult([message.RenderableResult], pickerNo, 0);
                    break;

                case EPrsInspectionUIUpdateType.ClearPickerResult:
                case EPrsInspectionUIUpdateType.ClearAllResults:
                    if (message is { PrsBody: { X1Orx2: 0 } })
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