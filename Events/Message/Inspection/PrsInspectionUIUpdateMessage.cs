using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Events.Message.Inspection
{
    public enum EPrsInspectionUIUpdateType
    {
        AddInspectionResult,
        UpdatePickerResult,

        ClearVisionWindow,
        ClearDeviceResults,
        ClearPickerResult,

        ClearAllResults,


        DisplayInspectionResult
    }

    public class PrsInspectionUIUpdateMessage
    {
        public EPrsInspectionUIUpdateType UpdateType;

        public RenderableInspectionResult? RenderableResult { get; set; }

        public int? PickerNo;

        public EachPrsBody? PrsBody;

        public int XPositionForShot => (int)(PrsBody?.XPickPosition ?? 0);

        public int YPositionForShot => (int)(PrsBody?.YPickPosition ?? 0);
    }
}
