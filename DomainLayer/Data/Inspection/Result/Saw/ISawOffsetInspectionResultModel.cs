using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Saw
{
    public interface ISawOffsetInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<SawOffset> SawOffset { get; set; }

        public ISawOffsetInspectionResultModel<T> MergeTo(ISawOffsetInspectionResultModel<T> model)
        {
            model.SawOffset = SawOffset;
            return model;
        }
    }
}
