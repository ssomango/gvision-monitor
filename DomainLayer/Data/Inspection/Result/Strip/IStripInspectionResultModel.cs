using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Strip
{
    public interface IStripInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<string> StripDataCode { get; set; }

        public Result<int>? XOffset { get; set; }

        public Result<int>? YOffset { get; set; }

        public IStripInspectionResultModel<T> MergeTo(IStripInspectionResultModel<T> model)
        {
            model.StripDataCode = StripDataCode;
            model.XOffset = XOffset;
            model.YOffset = YOffset;
            return model;
        }
    }
}
