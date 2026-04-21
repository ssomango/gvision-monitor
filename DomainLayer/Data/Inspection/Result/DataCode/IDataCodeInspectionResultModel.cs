using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.DataCode
{
    public interface IDataCodeInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<string> DataCode { get; set; }

        public IDataCodeInspectionResultModel<T> MergeTo(IDataCodeInspectionResultModel<T> model)
        {
            model.DataCode = DataCode;
            return model;
        }
    }
}
