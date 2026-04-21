using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Events.Message.Inspection
{
    public enum EMoldInspectionUIUpdateType
    {
        AddInspectionResult,
        UpdatePickerResult,

        ClearVisionWindow,
        ClearDeviceResults,

        ClearAllResults,


        DisplayInspectionResult
    }

    public class MoldInspectionUIUpdateMessage
    {
        public EMoldInspectionUIUpdateType UpdateType;

        public List<RenderableInspectionResult>? RenderableResults { get; set; }

        public MapBody? MapBody { get; set; }

        public int XPosition => (int)(MapBody?.XPosition ?? 0);

        public int YPosition => (int)(MapBody?.YPosition ?? 0);
    }
}
