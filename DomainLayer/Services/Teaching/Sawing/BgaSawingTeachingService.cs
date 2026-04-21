using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Ball;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.PresentationLayer.Communications;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.Sawing
{
    public sealed class BgaSawingTeachingService<TTeaching, TResult, TItem> : ISawingTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
        private IFirstPinItemProvider<TItem>? firstPinItemProvider = FirstPinItemProviderFactory.GetProvider<TItem>();
        private IPatternItemProvider<TItem>? patternItemProvider = PatternItemProviderFactory.GetProvider<TItem>();
        private IBallItemProvider<TItem>? ballItemProvider = BallItemProviderFactory.GetProvider<TItem>();
        private ISawingItemProvider<TItem>? sawingItemProvider = SawingItemProviderFactory.GetProvider<TItem>();

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        private HObject preprocessInspectionImage(AlignContext alignContext, TTeaching teaching, HashSet<TItem> inspectionItems, out HObject firstPinRegion, out HObject patternRegion, out HObject ballRegions)
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
            HOperatorSet.GenEmptyRegion(out firstPinRegion);
            if ((firstPinItemProvider is not null && inspectionItems.Contains(firstPinItemProvider.FirstPin))
                && teaching is IFirstPinTeachingModel<TTeaching> { FirstPinRoi : not null } firstPinTeaching)
            {
                VisionEngine.InspectFirstPin(
                    image: inspectionImage,
                    roi: firstPinTeaching.FirstPinRoi,
                    threshold: firstPinTeaching.FirstPinThreshold,
                    type: firstPinTeaching.FirstPinType,
                    out firstPinRegion
                );

                inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 2);
            }

            // Omit Pattern
            HOperatorSet.GenEmptyRegion(out patternRegion);
            if ((patternItemProvider is not null && inspectionItems.Contains(patternItemProvider.Pattern))
                && teaching is IPatternTeachingModel<TTeaching> patternTeaching && !patternTeaching.PatternRois.IsNullOrEmpty())
            {
                VisionEngine.InspectPattern(inspectionImage, patternTeaching.Patterns, patternTeaching.PatternThreshold, out patternRegion);
                inspectionImage = inspectionImage.OmitRegionFromTarget(patternRegion, 2);
            }

            // Omit Ball
            
            HOperatorSet.GenEmptyRegion(out ballRegions);
            if (teaching is IBallTeachingModel<TTeaching> ballTeaching && !ballTeaching.BallRois.IsNullOrEmpty())
            {
                BgaEngine.FindBalls(inspectionImage, ballTeaching.BallRois.ToList(), ballTeaching.BallThreshold, 1000, 9999, ballTeaching.BallMinCircularity, ballTeaching.BallPositionOffset,
                         out ballRegions, out Dictionary<string, List<Circle>> ballsByRoi);

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.MissingBall))
                {
                    BgaEngine.InspectMissingBall(inspectionImage, ballRegions, ballTeaching.BallRois.ToList(), ballTeaching.Balls, ballTeaching.BallThreshold, ballTeaching.BallMinArea, ballTeaching.BallMaxArea, out HObject missingBallRegion, out _);
                    VisionEngine.OmitRegionFromTarget(inspectionImage, missingBallRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, missingBallRegion, out ballRegions);
                    missingBallRegion.Dispose();
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.ExtraBall))
                {
                    BgaEngine.InspectExtraBall(ballRegions, ballTeaching.Balls, out HObject extraBallRegion);
                    VisionEngine.OmitRegionFromTarget(inspectionImage, extraBallRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, extraBallRegion, out ballRegions);
                    extraBallRegion.Dispose();
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallBridging))
                {
                    BgaEngine.InspectBallBridging(inspectionImage, ballTeaching.BallRois.ToList(), ballTeaching.Balls, ballTeaching.BallThreshold, ballTeaching.BallMinArea, ballTeaching.BallMaxArea, out HObject ballBridgingRegion);
                    VisionEngine.OmitRegionFromTarget(inspectionImage, ballBridgingRegion, 2, out inspectionImage);

                    HOperatorSet.SelectShapeProto(ballRegions, ballBridgingRegion, out HObject overlappedRegion, "overlaps_abs", 1, 1e10);
                    VisionOperation.Difference(ballRegions, overlappedRegion, out ballRegions);
                    overlappedRegion.Dispose();

                    VisionOperation.Difference(ballRegions, ballBridgingRegion, out ballRegions);
                    ballBridgingRegion.Dispose();
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallPitch))
                {
                    BgaEngine.InspectBallPitch(ballTeaching.BallRois.ToList(), ballsByRoi, ballRegions, ballTeaching.BallPitchesByRoi, out HObject wrongPitchRegion, out HObject edgeRegion);
                    VisionEngine.OmitRegionFromTarget(inspectionImage, wrongPitchRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, wrongPitchRegion, out ballRegions);
                    wrongPitchRegion.Dispose();
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallSize))
                {
                    double BallDiameterStandard = GlobalSetting.Instance.Inspection.BallDiameters;
                    var uodatedBallDiameterStandard = ballTeaching.BallDiametersByRoi.ToDictionary(pair => pair.Key, _ => (BallDiameterStandard));

                    BgaEngine.InspectBallSize(ballRegions, ballsByRoi, uodatedBallDiameterStandard, out HObject wrongSizeBallRegion);
                    VisionEngine.OmitRegionFromTarget(inspectionImage, wrongSizeBallRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, wrongSizeBallRegion, out ballRegions);
                    wrongSizeBallRegion.Dispose();
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallPosition))
                {
                    List<Circle> ballCircles = ballsByRoi.Values.SelectMany(circles => circles).ToList();
                    BgaEngine.InspectBallPosition(ballRegions, ballCircles, ballTeaching.Balls, out HObject wrongPositionBallRegion);
                    VisionEngine.OmitRegionFromTarget(inspectionImage, wrongPositionBallRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, wrongPositionBallRegion, out ballRegions);
                    wrongPositionBallRegion.Dispose();
                }
            }
           
            return inspectionImage;
        }

        public ISawingTeachingModel<TTeaching> TeachSawOffset(HObject teachingImage, AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ISawingTeachingModel<TTeaching> inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            HObject inspectionImage;

            if (teaching is ISinglePackageTeachingModel<TTeaching>)
            {
                HOperatorSet.ErosionCircle(alignContext.PackageRegion, out HObject erodedPackageRegion, 2);
                VisionOperation.ReduceDomain(alignContext.AlignedImage, erodedPackageRegion, out inspectionImage);
            }
            else if (teaching is IGridPackageTeachingModel<TTeaching>)
            {
                inspectionImage = alignContext.AlignedImage;
            }
            else
            {
                throw new Exception($"Unsupported teaching model type: {teaching.GetType()}");
            }
        

            HOperatorSet.GenEmptyRegion(out HObject firstPinRegion);
            HOperatorSet.GenEmptyRegion(out HObject patternRegion);
            HOperatorSet.GenEmptyRegion(out HObject ballRegion);

            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching)
            {
                inspectionImage = inspectionImage.OmitRegionFromTarget(dontCareTeaching.DontCareRois.ToList(), 1);
            }

            if (teaching is IFirstPinTeachingModel<TTeaching> firstPinTeaching)
            {
                VisionEngine.InspectFirstPin(inspectionImage, firstPinTeaching.FirstPinRoi, firstPinTeaching.FirstPinThreshold, EFirstPin.SmallPad, out firstPinRegion);
                firstPinRegion.DisposeBy(DisposeBag);

                inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 1);
            }

            if (teaching is IPatternTeachingModel<TTeaching> patternTeaching)
            {
                BgaEngine.InspectPattern(inspectionImage, patternTeaching.Patterns, patternTeaching.PatternThreshold, out patternRegion);
                patternRegion.DisposeBy(DisposeBag);
            }

            if (teaching is IBallTeachingModel<TTeaching> ballTeaching)
            {
                VisionOperation.GetBallRegionByThreshold(inspectionImage, ballTeaching.BallThreshold, (double)ballTeaching.BallMinCircularity / 100, out ballRegion);
                ballRegion.DisposeBy(DisposeBag);

                inspectionImage = inspectionImage.OmitRegionFromTarget(ballRegion, 2);
            }

            foreach (var sawOffsetItem in inspectedTeaching.SawOffsetItems)
            {
                HOperatorSet.GenEmptyRegion(out HObject targetRegion);

                switch (sawOffsetItem.SelectedSawOffsetStandardObject)
                {
                    case ESawOffsetStandardObject.FirstPin:
                        HOperatorSet.SelectRegionPoint(firstPinRegion, out targetRegion, sawOffsetItem.SawOffsetTargetPoint.Row, sawOffsetItem.SawOffsetTargetPoint.Col);
                        break;
                    case ESawOffsetStandardObject.Pattern:
                        HOperatorSet.SelectRegionPoint(patternRegion, out targetRegion, sawOffsetItem.SawOffsetTargetPoint.Row, sawOffsetItem.SawOffsetTargetPoint.Col);
                        break;
                    case ESawOffsetStandardObject.Ball:
                        HOperatorSet.FillUp(ballRegion, out HObject fillUpBallRegion);
                        fillUpBallRegion.DisposeBy(DisposeBag);
                        HOperatorSet.Connection(fillUpBallRegion, out fillUpBallRegion);
                        HOperatorSet.SelectRegionPoint(fillUpBallRegion, out targetRegion, sawOffsetItem.SawOffsetTargetPoint.Row, sawOffsetItem.SawOffsetTargetPoint.Col);
                        break;

                    default:
                        break;
                }

                foreach (EDirection direction in sawOffsetItem.Directions)
                {
                    VisionOperation.GetLineFromPackagePoints(alignContext.PackagePoints, direction, out Point start, out Point end);
                    VisionOperation.Distance(start, end, targetRegion, out Length pxDistance, out _);
                    sawOffsetItem.TaughtDistances[direction] = pxDistance.ConvertFromPixel(camera).Value;
                }

                renderData.ResultDrawings.Add((drawingObject: targetRegion, color: EColor.Green));
                targetRegion.DisposeBy(DisposeBag);
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
                    out HObject firstPinRegion,
                    out HObject patternRegion,
                    out _
                    );

                HOperatorSet.GenEmptyRegion(out HObject dontCareRegion);
                if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching)
                {
                    VisionOperation.Rois2Regions(dontCareTeaching.DontCareRois.ToList(), out dontCareRegion);
                    VisionOperation.AffineTransformRegion(dontCareRegion, alignContext.TransformMatrixInvert, out dontCareRegion);
                    dontCareRegion = dontCareRegion.ConcatObj(firstPinRegion).ConcatObj(patternRegion);
                }

                result.Burr = VisionEngine.InspectBurr(
                    image: alignContext.AlignedImage, 
                    packageRegion: alignContext.PackageRegion,
                    dontCareRegion: dontCareRegion, 
                    threshold: teaching.OutlineThreshold,
                    outlineWidth: teaching.OutlineWidth,
                    minLengthOfShortSide: teaching.MinLengthOfShortSide, 
                    maxLengthOfShortSide: teaching.MaxLengthOfShortSide,
                    minLengthOfLongSide: teaching.MinLengthOfLongSide,
                    maxLengthOfLongSide: teaching.MaxLengthOfLongSide, 
                    cameraType: camera, 
                    out HObject region
                    );

                dontCareRegion.Dispose();
                firstPinRegion.Dispose();
                patternRegion.Dispose();

                if (result.Burr.Type != EResultType.Good)
                {
                    renderData.ResultDrawings.Add((drawingObject: region, color: EResultType.Burr.GetResultColor((InspectionTeaching)teaching)));
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
                    out HObject firstPinRegion,
                    out HObject patternRegion,
                    out _
                    );

                HOperatorSet.GenEmptyRegion(out HObject dontCareRegion);
                if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching)
                {
                    VisionOperation.Rois2Regions(dontCareTeaching.DontCareRois.ToList(), out dontCareRegion);
                    VisionOperation.AffineTransformRegion(dontCareRegion, alignContext.TransformMatrixInvert, out dontCareRegion);
                    dontCareRegion = dontCareRegion.ConcatObj(firstPinRegion).ConcatObj(patternRegion);
                }

                result.Chipping = VisionEngine.InspectChipping(
                    image: alignContext.AlignedImage,
                    packageRegion: alignContext.PackageRegion,
                    dontCareRegion: dontCareRegion,
                    threshold: teaching.OutlineThreshold,
                    outlineWidth: teaching.OutlineWidth,
                    minLengthOfShortSide: teaching.MinLengthOfShortSide,
                    maxLengthOfShortSide: teaching.MaxLengthOfShortSide,
                    minLengthOfLongSide: teaching.MinLengthOfLongSide,
                    maxLengthOfLongSide: teaching.MaxLengthOfLongSide,
                    cameraType: camera,
                    out HObject region
                    );

                dontCareRegion.Dispose();
                firstPinRegion.Dispose();
                patternRegion.Dispose();

                if (result.Chipping.Type != EResultType.Good)
                {
                    renderData.ResultDrawings.Add((drawingObject: region, color: EResultType.Burr.GetResultColor((InspectionTeaching)teaching)));
                }
            }

            return result;
        }

        public ICornerDegreeInspectionResultModel<TResult> InspectCornerDegree(AlignContext alignContext, double tolerance, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ICornerDegreeInspectionResultModel<TResult> result = (ICornerDegreeInspectionResultModel<TResult>)(new TResult());
            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.CornerDegree)) || enforceAllChecks)
            {
                result.CornerDegree = BgaEngine.InspectCornerDegree(alignContext.PackagePoints, out _, out _);
            }
          
            return result;
        }

        public ISawOffsetInspectionResultModel<TResult> InspectSawOffset(AlignContext alignContext, double xTolerance, double yTolerance, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ISawOffsetInspectionResultModel<TResult> result = (ISawOffsetInspectionResultModel<TResult>)(new TResult());
            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.SawOffset)) || enforceAllChecks)
            {
                preprocessInspectionImage(
                  alignContext: alignContext,
                  teaching: (TTeaching)teaching,
                  inspectionItems: inspectionItems,
                  out HObject firstPinRegion,
                  out HObject patternRegion,
                  out HObject ballRegion
                  );

                result.SawOffset = BgaEngine.InspectSawOffset(teaching.SawOffsetItems.ToList(), alignContext.PackagePoints, firstPinRegion, patternRegion, ballRegion, out HObject footOfPerpendicular);

                using (footOfPerpendicular)
                {
                    HObject footOfPerpendicularTrans = footOfPerpendicular
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    renderData.ResultDrawings.Add((drawingObject: footOfPerpendicularTrans, color: EColor.Green));

                    firstPinRegion.Dispose();
                    patternRegion.Dispose();
                    ballRegion.Dispose();
                }
            }

            return result;
        }
    }
}
