using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Surface
{
    public interface IContaminationInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> Contamination { get; set; }

        public IContaminationInspectionResultModel<T> MergeTo(IContaminationInspectionResultModel<T> model)
        {
            model.Contamination = Contamination;
            return model;
        }
    }
}
