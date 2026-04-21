using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data;

namespace GVisionWpf.DomainLayer.Services.Teaching.Sawing
{
    public sealed partial class LeadAndPadSawingTeachingService<TTeaching, TResult, TItem> : ISawingTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private ISawingItemProvider<TItem>? sawingItemProvider = SawingItemProviderFactory.GetProvider<TItem>();
        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
        private IFirstPinItemProvider<TItem>?
            firstPinItemProvider = FirstPinItemProviderFactory.GetProvider<TItem>();

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        private HObject preprocessInspectionImage(AlignContext alignContext, TTeaching teaching, HashSet<TItem> inspectionItems, out HObject padRegion, out HObject leadRegion)
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
            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching &&
                !dontCareTeaching.DontCareRois.IsNullOrEmpty())
            {
                inspectionImage = inspectionImage.OmitRegionFromTarget(dontCareTeaching.DontCareRois.ToList(), 2);
            }

            // Omit RejectMark
            if ((rejectMarkItemProvider is not null && inspectionItems.Contains(rejectMarkItemProvider.RejectMark))
                && teaching is IRejectMarkTeachingModel<TTeaching> { RejectMarkRoi : not null } rejectMarkTeaching)
            {
                VisionEngine.InspectRejectMark(
                    image: inspectionImage,
                    roi: rejectMarkTeaching.RejectMarkRoi,
                    threshold: rejectMarkTeaching.RejectMarkThreshold,
                    minSize: rejectMarkTeaching.RejectMarkMinSize,
                    maxSize: rejectMarkTeaching.RejectMarkMaxSize,
                    out HObject rejectMarkRegion
                    );

                using (rejectMarkRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(rejectMarkRegion, 2);
                }
            }

            // Omit FirstPin
            if ((firstPinItemProvider is not null && inspectionItems.Contains(firstPinItemProvider.FirstPin))
                && teaching is IFirstPinTeachingModel<TTeaching> { FirstPinRoi : not null } firstPinTeaching)
            {
                VisionEngine.InspectFirstPin(
                    image: inspectionImage,
                    roi: firstPinTeaching.FirstPinRoi,
                    threshold: firstPinTeaching.FirstPinThreshold,
                    type: firstPinTeaching.FirstPinType,
                    out HObject firstPinRegion
                    );

                using (firstPinRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 2);
                }
            }

            // Omit Pad
            HOperatorSet.GenEmptyRegion(out padRegion);
            if (teaching is IMultiPadTeachingModel<TTeaching> multiPadTeaching && !multiPadTeaching.PadRois.ToList().IsNullOrEmpty())
            {
                switch (teaching)
                {
                    case LgaTeaching:
                    case GridLgaTeaching:
                        LgaEngine.FindMultiPad(inspectionImage, multiPadTeaching.PadRois.ToList(), multiPadTeaching.MultiPadThreshold, out padRegion);
                        inspectionImage = inspectionImage.OmitRegionFromTarget(padRegion, 2);
                        padRegion.DisposeBy(DisposeBag);
                        break;

                    default:
                        throw new NotSupportedException($"FindMultiPad is not supported for teaching type: {teaching.GetType().Name}");
                }
            }
            else if (teaching is ISinglePadTeachingModel<TTeaching> singlePadTeaching && singlePadTeaching.PadRoi != null)
            {
                switch (teaching)
                {
                    case QfnTeaching:
                    case GridQfnTeaching:
                        QfnEngine.FindPad(inspectionImage, singlePadTeaching.PadRoi, singlePadTeaching.PadThreshold, out padRegion);
                        inspectionImage = inspectionImage.OmitRegionFromTarget(padRegion, 2);
                        padRegion.DisposeBy(DisposeBag);
                        break;

                    default:
                        throw new NotSupportedException($"FindPad is not supported for teaching type: {teaching.GetType().Name}");
                }
            }

            // Omit Lead
            HOperatorSet.GenEmptyRegion(out leadRegion);
            if (teaching is ILeadTeachingModel<TTeaching> leadTeaching && !leadTeaching.LeadRois.IsNullOrEmpty())
            {
                switch (teaching)
                {
                    case LgaTeaching:
                    case GridLgaTeaching:
                        LgaEngine.FindLeads(inspectionImage, leadTeaching.LeadRois.ToList(), leadTeaching.LeadThreshold, out leadRegion);
                        break;

                    case QfnTeaching:
                    case GridQfnTeaching:
                        QfnEngine.FindLeads(inspectionImage, leadTeaching.LeadRois.ToList(), leadTeaching.LeadThreshold, out leadRegion);
                        break;

                    default:
                        throw new NotSupportedException($"FindLeads is not supported for teaching type: {teaching.GetType().Name}");
                }

                inspectionImage = inspectionImage.OmitRegionFromTarget(leadRegion, 2);
                leadRegion.DisposeBy(DisposeBag);
            }

            return inspectionImage;
        }

        public ISawingTeachingModel<TTeaching> TeachSawOffset(HObject teachingImage, AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            preprocessInspectionImage(
                alignContext: alignContext,
                teaching: (TTeaching)teaching,
                inspectionItems: inspectionItems,
                out HObject padRegion,
                out HObject leadRegion
                );

            foreach (var sawOffsetItem in inspectedTeaching.SawOffsetItems)
            {
                HOperatorSet.GenEmptyRegion(out HObject targetRegion);
                targetRegion.DisposeBy(DisposeBag);

                if (sawOffsetItem.SelectedSawOffsetStandardObject == ESawOffsetStandardObject.Pad)
                {
                    using (padRegion)
                    {
                        HOperatorSet.SelectRegionPoint(padRegion, out targetRegion, sawOffsetItem.SawOffsetTargetPoint.Row, sawOffsetItem.SawOffsetTargetPoint.Col);
                    }
                }
                else if (sawOffsetItem.SelectedSawOffsetStandardObject == ESawOffsetStandardObject.Lead)
                {
                    using (leadRegion)
                    {
                        HOperatorSet.SelectRegionPoint(leadRegion, out targetRegion, sawOffsetItem.SawOffsetTargetPoint.Row, sawOffsetItem.SawOffsetTargetPoint.Col);
                    }
                }

                foreach (EDirection direction in sawOffsetItem.Directions)
                {
                    VisionOperation.GetLineFromPackagePoints(alignContext.PackagePoints, direction, out Point start, out Point end);
                    VisionOperation.Distance(start, end, targetRegion, out Length distance, out _);
                    sawOffsetItem.TaughtDistances[direction] = distance.ConvertFromPixel(camera).Value;
                }

                using (targetRegion)
                {
                    HObject targetRegionTrans = targetRegion
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    renderData.ResultDrawings.Add((drawingObject: targetRegionTrans, color: EColor.Green));
                }

            }

            return inspectedTeaching;
        }

        public IBurrInspectionResultModel<TResult> InspectBurr(AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IBurrInspectionResultModel<TResult> result = (IBurrInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.Burr)) || enforceAllChecks)
            {
                preprocessInspectionImage(
                    alignContext: alignContext,
                    teaching: (TTeaching)teaching,
                    inspectionItems: inspectionItems,
                    out _,
                    out HObject leadRegion
                    );

                using (leadRegion)
                {
                    HObject leadRegioTrans = leadRegion
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    HOperatorSet.FillUp(leadRegioTrans, out HObject fillUpLeadRegion);
                    VisionOperation.Stretch(fillUpLeadRegion, 10, out HObject stretchedLeadRegion);
                    fillUpLeadRegion.Dispose();

                    result.Burr = VisionEngine.InspectBurr(
                        image: alignContext.AlignedImage,
                        packageRegion: alignContext.PackageRegion,
                        dontCareRegion: stretchedLeadRegion,
                        threshold: teaching.OutlineThreshold,
                        outlineWidth: teaching.OutlineWidth,
                        minLengthOfShortSide: teaching.MinLengthOfShortSide,
                        maxLengthOfShortSide: teaching.MaxLengthOfShortSide,
                        minLengthOfLongSide: teaching.MinLengthOfLongSide,
                        maxLengthOfLongSide: teaching.MaxLengthOfLongSide,
                        cameraType: camera,
                        out HObject region
                        );

                    if (result.Burr.Type != EResultType.Good)
                    {
                        renderData.ResultDrawings.Add((drawingObject: region, color: EResultType.Burr.GetResultColor((InspectionTeaching)teaching)));
                    }

                    stretchedLeadRegion.Dispose();
                    region.DisposeBy(DisposeBag);
                }
            }

            return result;
        }

        public IChippingInspectionResultModel<TResult> InspectChipping(AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IChippingInspectionResultModel<TResult> result = (IChippingInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.Chipping)) || enforceAllChecks)
            {
                preprocessInspectionImage(
                    alignContext: alignContext,
                    teaching: (TTeaching)teaching,
                    inspectionItems: inspectionItems,
                    out _,
                    out HObject leadRegion
                    );

                using (leadRegion)
                {
                    HObject leadRegionTrans = leadRegion
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    HOperatorSet.FillUp(leadRegionTrans, out HObject fillUpLeadRegion);
                    VisionOperation.Stretch(fillUpLeadRegion, 10, out HObject stretchedLeadRegion);
                    fillUpLeadRegion.Dispose();

                    result.Chipping = VisionEngine.InspectChipping(
                        image: alignContext.AlignedImage,
                        packageRegion: alignContext.PackageRegion,
                        dontCareRegion: stretchedLeadRegion,
                        threshold: teaching.OutlineThreshold,
                        outlineWidth: teaching.OutlineWidth,
                        minLengthOfShortSide: teaching.MinLengthOfShortSide,
                        maxLengthOfShortSide: teaching.MaxLengthOfShortSide,
                        minLengthOfLongSide: teaching.MinLengthOfLongSide,
                        maxLengthOfLongSide: teaching.MaxLengthOfLongSide,
                        cameraType: camera,
                        out HObject region
                        );


                    if (result.Chipping.Type != EResultType.Good)
                    {
                        renderData.ResultDrawings.Add((drawingObject: region, color: EResultType.Chipping.GetResultColor((InspectionTeaching)teaching)));
                    }

                    stretchedLeadRegion.Dispose();
                    region.DisposeBy(DisposeBag);
                }
            }

            return result;
        }

        public ICornerDegreeInspectionResultModel<TResult> InspectCornerDegree(AlignContext alignContext, double tolerance, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ICornerDegreeInspectionResultModel<TResult> result = (ICornerDegreeInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.CornerDegree)) || enforceAllChecks)
            {
                result.CornerDegree = inspectCornerDegree(
                points: alignContext.PackagePoints,
                out HObject cornerPointsRegion,
                out _
                );

                renderData.ResultDrawings.Add((drawingObject: cornerPointsRegion, color: result.CornerDegree.Type.GetResultColor((InspectionTeaching)teaching)));
            }

            return result;
        }

        public ISawOffsetInspectionResultModel<TResult> InspectSawOffset(AlignContext alignContext, double xTolerance, double yTolerance, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ISawOffsetInspectionResultModel<TResult> result = (ISawOffsetInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();
            List<Point> points = alignContext.PackagePoints;

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.SawOffset)) || enforceAllChecks)
            {
                preprocessInspectionImage(
                alignContext: alignContext,
                teaching: (TTeaching)teaching,
                inspectionItems: inspectionItems,
                out HObject padRegion,
                out HObject leadRegion
                );

                if (alignContext.TransformMatrix != null)
                {
                    VisionOperation.AffineTransformPoints(alignContext.PackagePoints, alignContext.TransformMatrix, out points);
                }

                result.SawOffset = inspectNewSawOffset(
                    teachingSawOffsetItems: teaching.SawOffsetItems.ToList(),
                    packagePoints: points,
                    leadRegion: leadRegion,
                    padRegion: padRegion,
                    out HObject footOfPerpendicular
                    );

                using (footOfPerpendicular)
                {
                    HObject footOfPerpendicularTrans = footOfPerpendicular
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    renderData.ResultDrawings.Add((drawingObject: footOfPerpendicularTrans, color: EColor.Green));

                    padRegion.Dispose();
                    leadRegion.Dispose();
                }
            }

            return result;
        }
    }

    partial class LeadAndPadSawingTeachingService<TTeaching, TResult, TItem>
    {
        private Result<CornerDegree> inspectCornerDegree(List<Point> points, out HObject cornerPoint, out List<FloatingText> texts)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectCornerDegree(points, out cornerPoint, out texts);
            else if (typeof(QfnTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridQfnTeaching).IsAssignableFrom(typeof(TTeaching)))
                return QfnEngine.InspectCornerDegree(points, out cornerPoint, out texts);
            else
                throw new NotSupportedException($"inspectCornerDegree is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<SawOffset> inspectNewSawOffset(List<SawOffsetItem> teachingSawOffsetItems, List<Point> packagePoints, HObject leadRegion, HObject padRegion, out HObject footOfPerpendicular)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectNewSawOffset(teachingSawOffsetItems, packagePoints, leadRegion, padRegion, out footOfPerpendicular);
            else if (typeof(QfnTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridQfnTeaching).IsAssignableFrom(typeof(TTeaching)))
                return QfnEngine.InspectNewSawOffset(teachingSawOffsetItems, packagePoints, leadRegion, padRegion, out footOfPerpendicular);
            else
                throw new NotSupportedException($"inspectNewSawOffset is not supported for teaching type: {typeof(TTeaching).Name}");
        }
    }
}
