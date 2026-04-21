using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Surface
{
    public interface IForeignMaterialInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> ForeignMaterial { get; set; }

        public IForeignMaterialInspectionResultModel<T> MergeTo(IForeignMaterialInspectionResultModel<T> model)
        {
            model.ForeignMaterial = ForeignMaterial;
            return model;
        }
    }
}
