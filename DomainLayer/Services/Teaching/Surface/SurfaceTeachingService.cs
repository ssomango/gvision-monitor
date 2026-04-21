using ControlzEx.Standard;
using GVisionWpf.DomainLayer.Align;
using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Ball;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Surface;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.Visions;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.Surface
{
    public sealed class SurfaceTeachingService<TTeaching, TResult, TItem> : ISurfaceTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private ISurfaceItemProvider<TItem>? surfaceItemProvider = SurfaceItemProviderFactory.GetProvider<TItem>();
        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
        private IFirstPinItemProvider<TItem>? firstPinItemProvider = FirstPinItemProviderFactory.GetProvider<TItem>();
        private IPatternItemProvider<TItem>? patternItemProvider = PatternItemProviderFactory.GetProvider<TItem>();
        private IBallItemProvider<TItem>? ballItemProvider = BallItemProviderFactory.GetProvider<TItem>();

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        private HObject? scratchRegion;

        private HObject? foreignMaterialRegion;

        private HObject? contaminationRegion;

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
                && teaching is IFirstPinTeachingModel<TTeaching> { FirstPinRoi : not null } firstPinTeaching)
            {
                VisionEngine.InspectFirstPin(inspectionImage, firstPinTeaching.FirstPinRoi, firstPinTeaching.FirstPinThreshold, firstPinTeaching.FirstPinType, out HObject firstPinRegion);
                using (firstPinRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 2);
                }
            }

            // Omit Pattern
            if ((patternItemProvider is not null && inspectionItems.Contains(patternItemProvider.Pattern))
                && teaching is IPatternTeachingModel<TTeaching> patternTeaching && !patternTeaching.PatternRois.IsNullOrEmpty())
            {
                VisionEngine.InspectPattern(inspectionImage, patternTeaching.Patterns, patternTeaching.PatternThreshold, out HObject patternRegion);
                using (patternRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(patternRegion, 2);
                }
            }

            // Omit Pad
            if (teaching is IMultiPadTeachingModel<TTeaching> multiPadTeaching && !multiPadTeaching.PadRois.ToList().IsNullOrEmpty())
            {
                switch (teaching)
                {
                    case LgaTeaching:
                    case GridLgaTeaching:
                        LgaEngine.FindMultiPad(inspectionImage, multiPadTeaching.PadRois.ToList(), multiPadTeaching.MultiPadThreshold, out HObject padRegion);
                        inspectionImage = inspectionImage.OmitRegionFromTarget(padRegion, 2);
                        padRegion.Dispose();
                        break;

                    default:
                        throw new NotSupportedException($"FindMultiPad is not supported for teaching type: {teaching.GetType().Name}");
                }
            } 
            else if (teaching is ISinglePadTeachingModel<TTeaching> singlePadTeaching && singlePadTeaching.PadRoi is not null) 
            {
                switch (teaching)
                {
                    case QfnTeaching:
                    case GridQfnTeaching:
                        QfnEngine.FindPad(inspectionImage, singlePadTeaching.PadRoi, singlePadTeaching.PadThreshold, out HObject padRegion);
                        inspectionImage = inspectionImage.OmitRegionFromTarget(padRegion, 2);
                        padRegion.Dispose();
                        break;

                    default:
                        throw new NotSupportedException($"FindPad is not supported for teaching type: {teaching.GetType().Name}");
                }
            }

            // Omit Lead
            if (teaching is ILeadTeachingModel<TTeaching> leadTeaching && !leadTeaching.LeadRois.IsNullOrEmpty())
            {
                HObject leadRegion;

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
                leadRegion.Dispose();
            }

            // Omit Ball
            if (teaching is IBallTeachingModel<TTeaching> ballTeaching && !ballTeaching.BallRois.IsNullOrEmpty())
            {
                BgaEngine.FindBalls(inspectionImage, ballTeaching.BallRois.ToList(), ballTeaching.BallThreshold, 1000, 9999, ballTeaching.BallMinCircularity, ballTeaching.BallPositionOffset,
                         out HObject ballRegions, out Dictionary<string, List<Circle>> ballsByRoi);

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.MissingBall))
                {
                    BgaEngine.InspectMissingBall(inspectionImage, ballRegions, ballTeaching.BallRois.ToList(), ballTeaching.Balls, ballTeaching.BallThreshold, ballTeaching.BallMinArea, ballTeaching.BallMaxArea, out HObject missingBallRegion, out _);
                    inspectionImage = inspectionImage.OmitRegionFromTarget(missingBallRegion, 2);
                    VisionOperation.Difference(ballRegions, missingBallRegion, out ballRegions);
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.ExtraBall))
                {
                    BgaEngine.InspectExtraBall(ballRegions, ballTeaching.Balls, out HObject extraBallRegion);
                    inspectionImage = inspectionImage.OmitRegionFromTarget(extraBallRegion, 2);
                    VisionOperation.Difference(ballRegions, extraBallRegion, out ballRegions);

                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallBridging))
                {
                    BgaEngine.InspectBallBridging(inspectionImage, ballTeaching.BallRois.ToList(), ballTeaching.Balls, ballTeaching.BallThreshold, ballTeaching.BallMinArea, ballTeaching.BallMaxArea, out HObject ballBridgingRegion);

                    inspectionImage = inspectionImage.OmitRegionFromTarget(ballBridgingRegion, 2);

                    HOperatorSet.SelectShapeProto(ballRegions, ballBridgingRegion, out HObject overlappedRegion, "overlaps_abs", 1, 1e10);
                    VisionOperation.Difference(ballRegions, overlappedRegion, out ballRegions);
                    overlappedRegion.Dispose();

                    VisionOperation.Difference(ballRegions, ballBridgingRegion, out ballRegions);
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallPitch))
                {
                    BgaEngine.InspectBallPitch(ballTeaching.BallRois.ToList(), ballsByRoi, ballRegions, ballTeaching.BallPitchesByRoi, out HObject wrongPitchRegion, out HObject edgeRegion);
                    inspectionImage = inspectionImage.OmitRegionFromTarget(wrongPitchRegion, 2);
                    VisionOperation.Difference(ballRegions, wrongPitchRegion, out ballRegions);
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallSize))
                {
                    double BallDiameterStandard = GlobalSetting.Instance.Inspection.BallDiameters;
                    var uodatedBallDiameterStandard = ballTeaching.BallDiametersByRoi.ToDictionary(pair => pair.Key, _ => (BallDiameterStandard));

                    BgaEngine.InspectBallSize(ballRegions, ballsByRoi, uodatedBallDiameterStandard, out HObject wrongSizeBallRegion);
                    VisionEngine.OmitRegionFromTarget(inspectionImage, wrongSizeBallRegion, 2, out inspectionImage);

                    VisionOperation.Difference(ballRegions, wrongSizeBallRegion, out ballRegions);
                }

                if (ballItemProvider is not null && inspectionItems.Contains(ballItemProvider.BallPosition))
                {
                    List<Circle> ballCircles = ballsByRoi.Values.SelectMany(circles => circles).ToList();
                    BgaEngine.InspectBallPosition(ballRegions, ballCircles, ballTeaching.Balls, out HObject wrongPositionBallRegion);
                    inspectionImage = inspectionImage.OmitRegionFromTarget(wrongPositionBallRegion, 2);
                    VisionOperation.Difference(ballRegions, wrongPositionBallRegion, out ballRegions);
                }

                inspectionImage = inspectionImage.OmitRegionFromTarget(ballRegions, 2);

                ballRegions.Dispose();
            }

            return inspectionImage;
        }

        public IContaminationInspectionResultModel<TResult> InspectContaminations(AlignContext alignContext, IContaminationTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IContaminationInspectionResultModel<TResult> result = (IContaminationInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if (((surfaceItemProvider is not null && inspectionItems.Contains(surfaceItemProvider.Contamination)) || enforceAllChecks)
                && !teaching.SurfaceRois.IsNullOrEmpty())
            {
                HObject inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

                if (foreignMaterialRegion != null && foreignMaterialRegion.IsInitialized()) inspectionImage = inspectionImage.OmitRegionFromTarget(foreignMaterialRegion, 2);
                if (scratchRegion != null && scratchRegion.IsInitialized()) inspectionImage = inspectionImage.OmitRegionFromTarget(scratchRegion, 2);

                result.Contamination = VisionEngine.InspectContamination(
                    image: inspectionImage,
                    rois: teaching.SurfaceRois.ToList(),
                    threshold: teaching.ContaminationThreshold,
                    minSize: teaching.ContaminationMinSize,
                    maxSize: teaching.ContaminationMaxSize,
                    out HObject contaminationRegion
                    );

                this.contaminationRegion = contaminationRegion.DisposeBy(DisposeBag);

                if (result.Contamination.Type != EResultType.Good)
                {
                    renderData.ResultDrawings.Add((drawingObject: contaminationRegion, color: EResultType.Contamination.GetResultColor((InspectionTeaching)teaching)));
                }

                inspectionImage.Dispose();
            }

            return result;
        }

        public IForeignMaterialInspectionResultModel<TResult> InspectForeignMaterials(AlignContext alignContext, IForeignMaterialTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IForeignMaterialInspectionResultModel<TResult> result = (IForeignMaterialInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if (((surfaceItemProvider is not null && inspectionItems.Contains(surfaceItemProvider.ForeignMaterial)) || enforceAllChecks)
                && !teaching.SurfaceRois.IsNullOrEmpty())
            {
                HObject inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

                if (contaminationRegion != null && contaminationRegion.IsInitialized()) inspectionImage = inspectionImage.OmitRegionFromTarget(contaminationRegion, 2);
                if (scratchRegion != null && scratchRegion.IsInitialized()) inspectionImage = inspectionImage.OmitRegionFromTarget(scratchRegion, 2);

                result.ForeignMaterial = VisionEngine.InspectForeignMaterial(
                    image: inspectionImage,
                    rois: teaching.SurfaceRois.ToList(),
                    threshold: teaching.ForeignMaterialThreshold,
                    minSize: teaching.ForeignMaterialMinSize,
                    maxSize: teaching.ForeignMaterialMaxSize,
                    out HObject foreignMaterialRegion
                    );

                this.foreignMaterialRegion = foreignMaterialRegion.DisposeBy(DisposeBag);

                if (result.ForeignMaterial.Type != EResultType.Good)
                {
                    renderData.ResultDrawings.Add((drawingObject: foreignMaterialRegion, EResultType.ForeignMaterial.GetResultColor((InspectionTeaching)teaching)));
                }

                inspectionImage.Dispose();
            }

            return result;
        }

        public IScratchInspectionResultModel<TResult> InspectScratches(AlignContext alignContext, IScratchTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IScratchInspectionResultModel<TResult> result = (IScratchInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if (((surfaceItemProvider is not null && inspectionItems.Contains(surfaceItemProvider.Scratch)) || enforceAllChecks)
                && !teaching.SurfaceRois.IsNullOrEmpty())
            {
                HObject inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

                if (contaminationRegion != null && contaminationRegion.IsInitialized()) inspectionImage = inspectionImage.OmitRegionFromTarget(contaminationRegion, 2);
                if (foreignMaterialRegion != null && foreignMaterialRegion.IsInitialized()) inspectionImage = inspectionImage.OmitRegionFromTarget(foreignMaterialRegion, 2);

                result.Scratch = VisionEngine.InspectScratch(
                    image: inspectionImage,
                    rois: teaching.SurfaceRois.ToList(),
                    threshold: teaching.ScratchThreshold,
                    minSize: teaching.ScratchMinSize,
                    maxSize: teaching.ScratchMaxSize,
                    out HObject scratchRegion
                    );

                this.scratchRegion = scratchRegion.DisposeBy(DisposeBag);

                if (result.Scratch.Type != EResultType.Good)
                {
                    renderData.ResultDrawings.Add((drawingObject: scratchRegion, EResultType.Scratch.GetResultColor((InspectionTeaching)teaching)));
                }

                inspectionImage.Dispose();
            }

            return result;
        }

        public void ResetInspectionState()
        {
            this.scratchRegion = null;
            this.foreignMaterialRegion = null;
            this.contaminationRegion = null;
        }
    }
}
