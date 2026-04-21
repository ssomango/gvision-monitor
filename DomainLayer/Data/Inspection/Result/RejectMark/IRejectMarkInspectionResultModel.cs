using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.RejectMark
{
    public interface IRejectMarkInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> RejectMark { get; set; }

        public IRejectMarkInspectionResultModel<T> MergeTo(IRejectMarkInspectionResultModel<T> model)
        {
            model.RejectMark = RejectMark;
            return model;
        }
    }
}
