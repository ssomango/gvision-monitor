using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.MultiPad
{
    public interface IMultiPadPtichesResultModel<T> where T : InspectionResult
    {
        public Result<List<LengthStatsInRoi>> MultiPadPitch { get; set; }

        public IMultiPadPtichesResultModel<T> MergeTo(IMultiPadPtichesResultModel<T> model)
        {
            model.MultiPadPitch = MultiPadPitch;
            return model;
        }
    }

}
