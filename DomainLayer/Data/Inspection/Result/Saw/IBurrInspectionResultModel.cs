using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Saw
{
    public interface IBurrInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> Burr { get; set; }

        public IBurrInspectionResultModel<T> MergeTo(IBurrInspectionResultModel<T> model)
        {
            model.Burr = Burr;
            return model;
        }
    }
}
