using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Saw
{
    public interface IChippingInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> Chipping { get; set; }

        public IChippingInspectionResultModel<T> MergeTo(IChippingInspectionResultModel<T> model)
        {
            model.Chipping = Chipping;
            return model;
        }
    }
}
