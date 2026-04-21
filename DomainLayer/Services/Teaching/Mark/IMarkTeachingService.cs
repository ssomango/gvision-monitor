using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Mark;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.Mark
{
    public partial interface IMarkTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public IMarkTeachingModel<TTeaching> TeachMarks(HObject teachingImage, HObject packageRegion, IMarkTeachingModel<TTeaching> teaching, IDontCareTeachingModel<TTeaching>? dontcare, out InspectionRenderData renderData);

        public IMarkInspectionResultModel<TResult> InspectMarks(AlignContext alignContext, IMarkTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
    }
}
