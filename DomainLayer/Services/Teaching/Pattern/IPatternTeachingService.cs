using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Pattern;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.Pattern
{
    public interface IPatternTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public IPatternTeachingModel<TTeaching> TeachPatterns(HObject teachingImage, IPatternTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IPatternInspectionResultModel<TResult> InspectPatterns(AlignContext alignContext, IPatternTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

    }
}
