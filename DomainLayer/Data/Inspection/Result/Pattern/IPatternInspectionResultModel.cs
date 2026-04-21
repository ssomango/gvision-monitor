using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Pattern
{
    public interface IPatternInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> PatternCount { get; set; }

        public IPatternInspectionResultModel<T> MergeTo(IPatternInspectionResultModel<T> model)
        {
            model.PatternCount = PatternCount;
            return model;
        }
    }
}
