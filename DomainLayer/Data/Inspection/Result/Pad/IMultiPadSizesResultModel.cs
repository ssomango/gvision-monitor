using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.MultiPad
{
    public interface IMultiPadSizesResultModel<T> where T : InspectionResult
    {
        public Result<StatisticalList<Size>> MultiPadSize { get; set; }
        public IMultiPadSizesResultModel<T> MergeTo(IMultiPadSizesResultModel<T> model)
        {
            model.MultiPadSize = MultiPadSize;
            return model;
        }
    }
}
