using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;


namespace GVisionWpf.DomainLayer.Services.Teaching.Sawing
{
    public partial interface ISawingTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {

        public DisposeBag DisposeBag { get; }

        public ISawingTeachingModel<TTeaching> TeachSawOffset(HObject teachingImage, AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public ISawOffsetInspectionResultModel<TResult> InspectSawOffset(AlignContext alignContext, double xTolerance, double yTolerance, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IChippingInspectionResultModel<TResult> InspectChipping(AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IBurrInspectionResultModel<TResult> InspectBurr(AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public ICornerDegreeInspectionResultModel<TResult> InspectCornerDegree(AlignContext alignContext, double tolerance, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
    }
}
