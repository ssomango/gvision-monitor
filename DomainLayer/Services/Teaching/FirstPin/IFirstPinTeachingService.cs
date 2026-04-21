using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.FirstPin;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.FirstPin
{
    public interface IFirstPinTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public IFirstPinTeachingModel<TTeaching> TeachFirstPin(HObject teachingImage, IFirstPinTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IFirstPinInspectResultModel<TResult> InspectFirstPin(AlignContext alignContext, IFirstPinTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
    }
}
