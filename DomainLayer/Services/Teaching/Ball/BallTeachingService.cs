using GVisionWpf.DomainLayer.Data.Inspection.Item.Ball;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Ball;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Interfaces.Teaching.Ball;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern;
using GVisionWpf.Extensions;
using GVisionWpf.DomainLayer.Data.Alignment;
using log4net;
using GVisionWpf.GlobalStates;

namespace GVisionWpf.DomainLayer.Services.Teaching.Ball
{
    public class BallTeachingService<TTeaching, TResult, TItem> : IBallTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
        private IFirstPinItemProvider<TItem>? firstPinItemProvider = FirstPinItemProviderFactory.GetProvider<TItem>();
        private IPatternItemProvider<TItem>? patternItemProvider = PatternItemProviderFactory.GetProvider<TItem>();
        private IBallItemProvider<TItem>? ballItemProvider = BallItemProviderFactory.GetProvider<TItem>();

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        private static readonly ILog log = LogManager.GetLogger("BallInspection");

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

            // Omit FirstPin
            if ((firstPinItemProvider is not null && inspectionItems.Contains(firstPinItemProvider.FirstPin))
                && teaching is IFirstPinTeachingModel<TTeaching> { FirstPinRoi: not null } firstPinTeaching)
            {
                VisionEngine.InspectFirstPin(inspectionImage, firstPinTeaching.FirstPinRoi, firstPinTeaching.FirstPinThreshold, firstPinTeaching.FirstPinType, out HObject firstPinRegion);
                using (firstPinRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 2);
                }
            }

            // Omit Patterns
            if ((patternItemProvider is not null && inspectionItems.Contains(patternItemProvider.Pattern))
                && teaching is IPatternTeachingModel<TTeaching> patternTeaching && !patternTeaching.PatternRois.IsNullOrEmpty())
            {
                VisionEngine.InspectPattern(inspectionImage, patternTeaching.Patterns, patternTeaching.PatternThreshold, out HObject patternRegion);
                using (patternRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(patternRegion, 2);
                }
            }

            return inspectionImage;
        }

        public IBallTeachingModel<TTeaching> FindBallAutoRoi(HObject teachingImage, IBallTeachingModel<TTeaching> teaching)
        {
            var inspectedTeaching = DeepCopy.Copy(teaching);

            VisionOperation.FindBallRoiAuto(teachingImage, out Roi roi);

            inspectedTeaching.BallRois = [roi];

            return inspectedTeaching;
        }


        public IBallTeachingModel<TTeaching> TeachBalls(HObject TeachingImage, IBallTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            renderData = new InspectionRenderData();
            var inspectedTeaching = DeepCopy.Copy(teaching);

            BgaEngine.FindBalls(TeachingImage, teaching.BallRois.ToList(), teaching.BallThreshold, 300, 9999, teaching.BallMinCircularity, teaching.BallPositionOffset,
                                out HObject ballRegion, out Dictionary<string, List<Circle>> ballsByRoi);

            HOperatorSet.RegionFeatures(ballRegion, "area", out HTuple area);
            inspectedTeaching.BallMinArea = area.TupleMin().D;
            inspectedTeaching.BallMaxArea = area.TupleMax().D;
            inspectedTeaching.BallMinSize = area.TupleMin().D;
            inspectedTeaching.BallMaxSize = area.TupleMax().D;


            inspectedTeaching.Balls = ballsByRoi.Values.SelectMany(circles => circles).ToList();
            inspectedTeaching.BallDiametersByRoi = ballsByRoi.ToDictionary(pair => pair.Key, pair => pair.Value.Average(c => c.Radius * 2));
            inspectedTeaching.BallAvgDiameters = ballsByRoi.SelectMany(pair => pair.Value).Average(c => c.Radius * 2);


            Dictionary<string, Length> ballPitchesByRoi = new Dictionary<string, Length>();

            foreach (Roi roi in teaching.BallRois)
            {
                ballPitchesByRoi[roi.Name] = new Length(0);
            }

            /*
            Result<StatisticalList<Length>> ballPitch = BgaEngine.InspectBallPitch(teaching.BallRois.ToList(), ballsByRoi, ballRegion, ballPitchesByRoi, out HObject wrongPitchRegion, out HObject edgeRegion);
            
            foreach (var a in teaching.BallRois)
            {
                ballPitchesByRoi[a.Name] = ballPitch.Value.MemberwiseAverage();
            }
            */

            inspectedTeaching.BallPitchesByRoi = ballPitchesByRoi;
            ballRegion.DisposeBy(DisposeBag);

            renderData.ResultDrawings.Add((drawingObject: ballRegion, color: EColor.Green));

            return inspectedTeaching;
        }

        public IBallInspectionResultModel<TResult> InspectBalls(AlignContext alignContext, IBallTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IBallInspectionResultModel<TResult> result = (IBallInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            // 티칭 없으면 그냥 넘김
            if (teaching.Balls.IsNullOrEmpty()) return result;

            // ---------------  Ball Inspection start  -------------------------------

            HObject inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

            //HOperatorSet.ErosionCircle(alignContext.PackageRegion, out HObject erosionPackageRegion, 2);
            //VisionOperation.ReduceDomain(alignContext.AlignedImage, erosionPackageRegion, out HObject inspectionImage);
            //erosionPackageRegion.Dispose();

            if ((this.ballItemProvider is not null && (inspectionItems.Contains(this.ballItemProvider.BallCount)
              || inspectionItems.Contains(this.ballItemProvider.BallSize))
              || inspectionItems.Contains(this.ballItemProvider.BallPitch)
              || inspectionItems.Contains(this.ballItemProvider.BallBridging)
              || inspectionItems.Contains(this.ballItemProvider.ExtraBall)
              || inspectionItems.Contains(this.ballItemProvider.MissingBall)
              || inspectionItems.Contains(this.ballItemProvider.CrackBall)
              || inspectionItems.Contains(this.ballItemProvider.BallPosition)
              || inspectionItems.Contains(this.ballItemProvider.BallLight))
              )
            {
                BgaEngine.FindBalls(inspectionImage, teaching.BallRois.ToList(), teaching.BallThreshold, 300, 9999, teaching.BallMinCircularity, teaching.BallPositionOffset,
                             out HObject ballRegions, out Dictionary<string, List<Circle>> ballsByRoi);

                renderData.ResultDrawings.Add((drawingObject: ballRegions, EColor: EColor.Green));

                // Briging ball 
                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.BallBridging)) || enforceAllChecks)
                {
                    result.BallBridging = BgaEngine.InspectBallBridging(inspectionImage, teaching.BallRois.ToList(), teaching.Balls, teaching.BallThreshold, teaching.BallMinArea, teaching.BallMaxArea, out HObject ballBridgingRegion);
                    renderData.ResultDrawings.Add((drawingObject: ballBridgingRegion, color: EResultType.BallBridging.GetResultColor((InspectionTeaching)teaching)));
                    VisionEngine.OmitRegionFromTarget(inspectionImage, ballBridgingRegion, 2, out inspectionImage);

                    HOperatorSet.SelectShapeProto(ballRegions, ballBridgingRegion, out HObject overlappedRegion, "overlaps_abs", 1, 1e10);
                    VisionOperation.Difference(ballRegions, overlappedRegion, out ballRegions);
                    VisionOperation.Difference(ballRegions, ballBridgingRegion, out ballRegions);

                    overlappedRegion.Dispose();
                    ballBridgingRegion.DisposeBy(DisposeBag);
                }

                // MissingBall
                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.MissingBall)) || enforceAllChecks)
                {
                    result.MissingBall = BgaEngine.InspectMissingBall(inspectionImage, ballRegions, teaching.BallRois.ToList(), teaching.Balls, teaching.BallThreshold, teaching.BallMinArea * 0.8, teaching.BallMaxArea * 1.2, out HObject missingBallRegion, out HObject nomissingBallRegion);

                    renderData.ResultDrawings.Add((drawingObject: missingBallRegion, color: EResultType.MissingBall.GetResultColor((InspectionTeaching)teaching)));
                    renderData.ResultDrawings.Add((drawingObject: nomissingBallRegion, color: EColor.Green));

                    VisionOperation.Difference(ballRegions, missingBallRegion, out ballRegions);

                    missingBallRegion.DisposeBy(DisposeBag);
                    nomissingBallRegion.DisposeBy(DisposeBag);
                }

                // ExtraBall
                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.ExtraBall)) || enforceAllChecks)
                {
                    result.ExtraBall = BgaEngine.InspectExtraBall(ballRegions, teaching.Balls, out HObject extraBallRegion);
                    renderData.ResultDrawings.Add((drawingObject: extraBallRegion, color: EResultType.ExtraBall.GetResultColor((InspectionTeaching)teaching)));
                    VisionEngine.OmitRegionFromTarget(inspectionImage, extraBallRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, extraBallRegion, out ballRegions);

                    extraBallRegion.DisposeBy(DisposeBag);
                }

                // crack ball
                /*
                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.CrackBall)) || enforceAllChecks)
                {
                    result.CrackBall = BgaEngine.CrackBall(ballRegions, out HObject CrakBallRegions);
                    HObject transformed = CrakBallRegions.AffineTransformRegion(alignContext.TransformMatrixInvert).DisposeBy(DisposeBag);
                    renderData.ResultDrawings.Add((drawingObject: CrakBallRegions, color: EResultType.CrackBall.GetResultColor((InspectionTeaching)teaching)));
                    VisionEngine.OmitRegionFromTarget(inspectionImage, CrakBallRegions, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, CrakBallRegions, out ballRegions);
                }
                */

                //Ball Size 지름크기 : 픽셀로
                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.BallSize)) || enforceAllChecks)
                {
                    double BallDiameterStandard = GlobalSetting.Instance.Inspection.BallDiameters;
                    var uodatedBallDiameterStandard = teaching.BallDiametersByRoi.ToDictionary(pair => pair.Key, _ => (BallDiameterStandard));
                    result.BallSize = BgaEngine.InspectBallSize(ballRegions, ballsByRoi, uodatedBallDiameterStandard, out HObject wrongSizeBallRegion);
                    renderData.ResultDrawings.Add((drawingObject: wrongSizeBallRegion, color: EResultType.BallSize.GetResultColor((InspectionTeaching)teaching)));
                    VisionEngine.OmitRegionFromTarget(inspectionImage, wrongSizeBallRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, wrongSizeBallRegion, out ballRegions);

                    wrongSizeBallRegion.DisposeBy(DisposeBag);
                }

                //Ball pitch
                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.BallPitch)) || enforceAllChecks)
                {
                    Length PitchStandard = new Length(GlobalSetting.Instance.Inspection.BallPitch);

                    var updatedDict = teaching.BallPitchesByRoi.ToDictionary(pair => pair.Key, _ => (PitchStandard));
                    result.BallPitch = BgaEngine.InspectBallPitch(teaching.BallRois.ToList(), ballsByRoi, ballRegions, updatedDict, out HObject wrongPitchRegion, out HObject edgeRegion);
                    renderData.ResultDrawings.Add((drawingObject: edgeRegion, color: EResultType.BallPitch.GetResultColor((InspectionTeaching)teaching)));
                    VisionEngine.OmitRegionFromTarget(inspectionImage, wrongPitchRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, wrongPitchRegion, out ballRegions);

                    edgeRegion.DisposeBy(DisposeBag);
                    wrongPitchRegion.DisposeBy(DisposeBag);
                }

                // Ball Position
                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.BallPosition)) || enforceAllChecks)
                {
                    List<Circle> ballCircles = ballsByRoi.Values.SelectMany(circles => circles).ToList();
                    result.BallPosition = BgaEngine.InspectBallPosition(ballRegions, ballCircles, teaching.Balls, out HObject wrongPositionBallRegion);
                    renderData.ResultDrawings.Add((drawingObject: wrongPositionBallRegion, color: EResultType.BallPosition.GetResultColor((InspectionTeaching)teaching)));
                    VisionEngine.OmitRegionFromTarget(inspectionImage, wrongPositionBallRegion, 2, out inspectionImage);
                    VisionOperation.Difference(ballRegions, wrongPositionBallRegion, out ballRegions);

                    wrongPositionBallRegion.DisposeBy(DisposeBag);

                }

                if ((this.ballItemProvider is not null && inspectionItems.Contains(this.ballItemProvider.BallCount)) || enforceAllChecks)
                {
                    result.BallCount = BgaEngine.InspectBallCount(ballRegions, teaching.Balls.Count);
                }

                ballRegions.Dispose();
                inspectionImage.Dispose();
            }

            return result;
        }
    }
}