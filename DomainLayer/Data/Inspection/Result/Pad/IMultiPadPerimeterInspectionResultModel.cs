using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.MultiPad
{
    public interface IMultiPadPerimeterInspectionResultModel<T> where T : InspectionResult
    {
        public Result<StatisticalList<Length>> MultiPadPerimeter { get; set; }

        public IMultiPadPerimeterInspectionResultModel<T> MergeTo(IMultiPadPerimeterInspectionResultModel<T> model)
        {
            model.MultiPadPerimeter = MultiPadPerimeter;
            return model;
        }
    }
}
