using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Surface
{
    public interface IScratchInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> Scratch { get; set; }

        public IScratchInspectionResultModel<T> MergeTo(IScratchInspectionResultModel<T> model)
        {
            model.Scratch = Scratch;
            return model;
        }
    }
}
