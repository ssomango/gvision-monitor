using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.FirstPin
{
    public interface IFirstPinInspectResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<bool> FirstPin { get; set; }

        public IFirstPinInspectResultModel<T> MergeTo(IFirstPinInspectResultModel<T> model)
        {
            model.FirstPin = FirstPin;
            return model;
        }
    }
}
