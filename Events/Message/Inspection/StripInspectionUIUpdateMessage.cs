using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Events.Message.Inspection
{
    public enum EStripInspectionUIUpdateType
    {
        AddInspectionResult
    }

    public class StripInspectionUIUpdateMessage
    {
        public EStripInspectionUIUpdateType UpdateType;

        public RenderableInspectionResult? RenderableResult { get; set; }

        public ERequestInspectionType InspectionType { get; set; }
    }
}
