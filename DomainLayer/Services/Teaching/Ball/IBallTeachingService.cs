using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Ball;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Interfaces.Teaching.Ball
{
    public interface IBallTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public IBallTeachingModel<TTeaching> FindBallAutoRoi(HObject teachingImage, IBallTeachingModel<TTeaching> teaching);

        public IBallTeachingModel<TTeaching> TeachBalls(HObject teachingImage, IBallTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IBallInspectionResultModel<TResult> InspectBalls(AlignContext alignContext, IBallTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
    }
}
