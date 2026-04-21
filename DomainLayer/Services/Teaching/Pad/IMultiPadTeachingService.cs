using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Interfaces.MultiPad;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.MultiPad
{
    public interface IMultiPadTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public IMultiPadTeachingModel<TTeaching> TeachPads(HObject teachingImage, HObject packageRegion, ECamera camera, IMultiPadTeachingModel<TTeaching> teaching, out InspectionRenderData renderData);

        public IMultiPadInspectionResultModel<TResult> InspectPads(AlignContext alignContext, IMultiPadTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
    }
}
