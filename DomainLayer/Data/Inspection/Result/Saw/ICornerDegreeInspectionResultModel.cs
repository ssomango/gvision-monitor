using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Saw
{
    public interface ICornerDegreeInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<CornerDegree> CornerDegree { get; set; }

        public ICornerDegreeInspectionResultModel<T> MergeTo(ICornerDegreeInspectionResultModel<T> model)
        {
            model.CornerDegree = CornerDegree;
            return model;
        }
    }
}
