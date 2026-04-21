using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Pattern;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern;
using GVisionWpf.DomainLayer.Data.Alignment;

namespace GVisionWpf.DomainLayer.Services.Teaching.Pattern
{
    public class PatternTeachingInspectionService<TTeaching, TResult, TItem> : IPatternTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
        private IFirstPinItemProvider<TItem>? firstPinItemProvider = FirstPinItemProviderFactory.GetProvider<TItem>();
        private IPatternItemProvider<TItem>? patternItemProvider = PatternItemProviderFactory.GetProvider<TItem>();

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        private HObject preprocessInspectionImage(AlignContext alignContext, TTeaching teaching, HashSet<TItem> inspectionItems)
        {
            HObject inspectionImage;

            if (teaching is ISinglePackageTeachingModel<TTeaching> singlePackageTeaching)
            {
                HOperatorSet.ErosionCircle(alignContext.PackageRegion, out HObject erosionPackageRegion, 2);
                VisionOperation.ReduceDomain(alignContext.AlignedImage, erosionPackageRegion, out inspectionImage);
                erosionPackageRegion.Dispose();
            }
            else if (teaching is IGridPackageTeachingModel<TTeaching> gridPackageTeaching)
            {
                inspectionImage = alignContext.AlignedImage;
            }
            else
            {
                throw new Exception($"Unsupported teaching model type: {teaching.GetType()}");
            }

            // Omit DontCare
            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching
                && !dontCareTeaching.DontCareRois.IsNullOrEmpty())
            {
                inspectionImage = inspectionImage.OmitRegionFromTarget(dontCareTeaching.DontCareRois.ToList(), 2);
            }

            // Omit RejectMark
            if ((rejectMarkItemProvider is not null && inspectionItems.Contains(rejectMarkItemProvider.RejectMark))
                && teaching is IRejectMarkTeachingModel<TTeaching> { RejectMarkRoi: not null } rejectMarkTeaching)
            {
                VisionEngine.InspectRejectMark(inspectionImage, rejectMarkTeaching.RejectMarkRoi, rejectMarkTeaching.RejectMarkThreshold, rejectMarkTeaching.RejectMarkMinSize, rejectMarkTeaching.RejectMarkMaxSize, out HObject rejectMarkRegion);
                using (rejectMarkRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(rejectMarkRegion, 2);
                }
            }

            if ((firstPinItemProvider is not null && inspectionItems.Contains(firstPinItemProvider.FirstPin))
                && teaching is IFirstPinTeachingModel<TTeaching> { FirstPinRoi : not null } firstPinTeaching)
            {
                VisionEngine.InspectFirstPin(inspectionImage, firstPinTeaching.FirstPinRoi, firstPinTeaching.FirstPinThreshold, firstPinTeaching.FirstPinType, out HObject firstPinRegion);
                using (firstPinRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 2);
                }
            }

            return inspectionImage;
        }

        public IPatternTeachingModel<TTeaching> TeachPatterns(HObject teachingImage, IPatternTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            VisionOperation.FindRects(teachingImage, teaching.PatternRois.ToList(), teaching.PatternThreshold, out List<Rect> patternRects);
            VisionOperation.Rects2Rois(patternRects, out List<Roi> tmpRois);

            HObject patternRegion = tmpRois
                .Rois2Regions()
                .DisposeBy(DisposeBag);

            inspectedTeaching.Patterns = patternRects;

            renderData.ResultDrawings.Add((drawingObject: patternRegion, color: EColor.Green));

            return inspectedTeaching;
        }

        public IPatternInspectionResultModel<TResult> InspectPatterns(AlignContext alignContext, IPatternTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IPatternInspectionResultModel<TResult> result = (IPatternInspectionResultModel<TResult>)new TResult();

            renderData = new InspectionRenderData();

            if ((patternItemProvider is not null && inspectionItems.Contains(patternItemProvider.Pattern))
                || enforceAllChecks)
            {
                var inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

                result.PatternCount = VisionEngine.InspectPattern(inspectionImage, teaching.Patterns, teaching.PatternThreshold, out HObject patternRegion);

                HObject patternRegionTrans = patternRegion
                    .AffineTransformRegion(alignContext.TransformMatrixInvert)
                    .DisposeBy(DisposeBag);

                renderData.ResultDrawings.Add((drawingObject: patternRegionTrans, color: EColor.Green));
            }

            return result;
        }
    }
}
