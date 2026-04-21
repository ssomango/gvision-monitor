using GVisionWpf.Events.Message.Dialog;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Events.MessengerHandler
{
    public sealed class UIMessengerHandler
    {
        private readonly GVisionMessenger _messenger;

        public UIMessengerHandler(GVisionMessenger messenger)
        {
            _messenger = messenger;
        }

        public void SendSystemInfoMessage(string message)
        {
            _messenger.Send(message);
        }

        public void SendPrsUIUpdate(EPrsInspectionUIUpdateType updateType, RenderableInspectionResult? result = null, int? pickerNo = null, EachPrsBody? prsBody = null)
        {
            var message = new PrsInspectionUIUpdateMessage
            {
                UpdateType = updateType,
                RenderableResult = result,
                PickerNo = pickerNo,
                PrsBody = prsBody
            };

            _messenger.Send(message);
        }

        public void SendMappingUIUpdate(EMoldInspectionUIUpdateType updateType, List<RenderableInspectionResult>? results = null, MapBody? mapBody = null)
        {
            var message = new MoldInspectionUIUpdateMessage
            {
                UpdateType = updateType,
                RenderableResults = results,
                MapBody = mapBody
            };

            _messenger.Send(message);
        }

        public void SendStripUIUpdate(EStripInspectionUIUpdateType updateType, ERequestInspectionType inspectionType, RenderableInspectionResult? result = null)
        {
            if (inspectionType is ERequestInspectionType.TOP_BARCODE or ERequestInspectionType.BOTTOM_BARCODE)
            {
                var message = new StripInspectionUIUpdateMessage
                {
                    UpdateType = updateType,
                    InspectionType = inspectionType,
                    RenderableResult = result
                };

                _messenger.Send(message);
            }
        }
    }
}
