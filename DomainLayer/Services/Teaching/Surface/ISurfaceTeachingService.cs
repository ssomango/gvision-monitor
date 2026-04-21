using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.Surface
{
    public partial interface ISurfaceTeachingService<TTeahcing, TResult, TItem> : IDisposable
        where TTeahcing : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public IScratchInspectionResultModel<TResult> InspectScratches(AlignContext alignContext, IScratchTeachingModel<TTeahcing> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IForeignMaterialInspectionResultModel<TResult> InspectForeignMaterials(AlignContext alignContext, IForeignMaterialTeachingModel<TTeahcing> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IContaminationInspectionResultModel<TResult> InspectContaminations(AlignContext alignContext, IContaminationTeachingModel<TTeahcing> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
        void ResetInspectionState();
    }
}
