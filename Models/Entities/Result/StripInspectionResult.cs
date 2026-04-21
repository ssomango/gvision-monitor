using GVisionWpf.DomainLayer.Data.Inspection.Result.Strip;

namespace GVisionWpf.Models.Entities.Result
{
    public partial class StripInspectionResult : InspectionResult { }

    partial class StripInspectionResult : IStripInspectionResultModel<StripInspectionResult>
    {
        public Result<string> StripDataCode { get; set; } = new Result<string>(type: EResultType.DataCode, value: null);
        public Result<int>? XOffset { get; set; }
        public Result<int>? YOffset { get; set; }
    }
}