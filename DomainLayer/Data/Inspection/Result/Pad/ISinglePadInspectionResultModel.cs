using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Pad
{
    public interface ISinglePadInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<Size> PadSize { get; set; }
        public Result<Ratio> PadArea { get; set; }

        public ISinglePadInspectionResultModel<T> MergeTo(ISinglePadInspectionResultModel<T> model)
        {
            model.PadSize = this.PadSize;
            model.PadArea = this.PadArea;
            return model;
        }
    }
}
