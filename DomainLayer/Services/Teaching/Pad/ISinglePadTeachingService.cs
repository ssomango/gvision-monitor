using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Pad;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.Pad
{
    public interface ISinglePadTeachingService<TTeaching, TResult, TItem> : IDisposable
      where TTeaching : InspectionTeaching
      where TResult : InspectionResult
      where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public ISinglePadTeachingModel<TTeaching> TeachPad(HObject teachingImage, HObject packageRegion, ECamera camera, ISinglePadTeachingModel<TTeaching> teaching, out InspectionRenderData renderData);

        public ISinglePadInspectionResultModel<TResult> InspectPad(AlignContext alignContext, ISinglePadTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
    }
}
