using GVisionWpf.Interfaces.Inspect.Lead;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Align;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Lead;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data;

namespace GVisionWpf.DomainLayer.Services.Teaching.Lead
{
    public partial class LeadTeachingService<TTeaching, TResult, TItem> : ILeadTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
        private IFirstPinItemProvider<TItem>? firstPinItemProvider = FirstPinItemProviderFactory.GetProvider<TItem>();
        private ILeadItemProvider<TItem>? leadItemProvider = LeadItemProviderFactory.GetProvider<TItem>();


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
                && teaching is IRejectMarkTeachingModel<TTeaching> { RejectMarkRoi : not null } rejectMarkTeaching)
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
                inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 2);
                firstPinRegion.Dispose();
            }

            // Omit Pad
            if (teaching is IMultiPadTeachingModel<TTeaching> multiPadTeaching)
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
            else if (teaching is ISinglePadTeachingModel<TTeaching> singlePadTeaching)
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

            return inspectionImage;
        }

        public ILeadTeachingModel<TTeaching> TeachLeads(HObject teachingImage, HObject packageRegion, ECamera camera, ILeadTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            if (teaching.LeadRois.IsNullOrEmpty()) return inspectedTeaching;

            HObject inspectionImage;

            if (teaching is ISinglePackageTeachingModel<TTeaching>)
            {
                HOperatorSet.ErosionCircle(packageRegion, out packageRegion, 2);
                VisionOperation.ReduceDomain(teachingImage, packageRegion, out inspectionImage);
            }
            else if (teaching is IGridPackageTeachingModel<TTeaching>)
            {
                inspectionImage = teachingImage;
            }
            else
            {
                throw new Exception($"Unsupported teaching model type: {teaching.GetType()}");
            }
          

            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching &&
                dontCareTeaching.DontCareRois.Count > 0)
            {
                VisionOperation.ReduceDomainComplement(inspectionImage, dontCareTeaching.DontCareRois.ToList(), out inspectionImage);
            }


            findLeads(inspectionImage, teaching.LeadRois.ToList(), teaching.LeadThreshold, out HObject leadRegions);

            using (leadRegions)
            {
                VisionOperation.GetRegionOrientationOfSmallestRectangle2(leadRegions, out List<Pose> leadPxPoses, out _);
                inspectedTeaching.LeadPxPoses = leadPxPoses;

                VisionOperation.GetAverageArea(leadRegions, out int leadAvgArea);
                inspectedTeaching.LeadAverageArea = leadAvgArea;

                #region LeadPitches
                var leadPitches = inspectLeadPitches(
                    leadsRegion: leadRegions,
                    leadRois: teaching.LeadRois.ToList()
                    ).Value;

                inspectedTeaching.LeadAveragePitch = leadPitches?.Select(pitches => pitches.MemberwiseAverage().Value).Average() ?? 0;
                #endregion

                #region LeadSizes
                var leadSizes = inspectLeadSizes(
                    leadsRegion: leadRegions,
                    originalSize: new Size(0, 0)
                    ).Value;

                inspectedTeaching.LeadAverageSize = new Size(
                    width: leadSizes?.MemberwiseAverage().Width ?? 0,
                    height: leadSizes?.MemberwiseAverage().Height ?? 0
                    );
                #endregion

                #region LeadPerimeter
                var leadPerimeter = inspectLeadPerimeter(
                    region: leadRegions,
                    avgPerimeter: 0,
                    out _).Value;

                inspectedTeaching.LeadAveragePerimeter = leadPerimeter?.MemberwiseAverage().Value ?? 0;
                #endregion

                HOperatorSet.FillUp(leadRegions, out HObject fillUpLeadRegion);
                fillUpLeadRegion.DisposeBy(DisposeBag);

                renderData.ResultDrawings.Add((drawingObject: fillUpLeadRegion, color: EColor.Green));

                return inspectedTeaching;
            }
        }

        public ILeadInspectionResultModel<TResult> InspectLeads(AlignContext alignContext, ILeadTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ILeadInspectionResultModel<TResult> leadResult = (ILeadInspectionResultModel<TResult>)new TResult();

            renderData = new InspectionRenderData();

            HObject inspectionImage = preprocessInspectionImage(
                alignContext: alignContext,
                teaching: (TTeaching)teaching,
                inspectionItems: inspectionItems
                );

            findLeads(inspectionImage, teaching.LeadRois.ToList(), teaching.LeadThreshold, out HObject leadRegion);

            using (leadRegion)
            {
                HOperatorSet.FillUp(leadRegion, out HObject fillUpPadRegion);

                HObject fillUpPadRegionTrans = fillUpPadRegion
                    .AffineTransformRegion(alignContext.TransformMatrixInvert)
                    .DisposeBy(DisposeBag);

                renderData.ResultDrawings.Add((drawingObject: fillUpPadRegionTrans, EColor.Green));

                using (fillUpPadRegion)
                {
                    if (leadItemProvider is not null)
                    {
                        if (inspectionItems.Contains(leadItemProvider.LeadCount) || enforceAllChecks)
                        {
                            int leadCount = VisionOperation.GetCountOf(fillUpPadRegion);

                            leadResult.LeadCount = new Result<int>(
                                type: leadCount == teaching.LeadPxPoses.Count() ? EResultType.Good : EResultType.LeadCount,
                                value: leadCount
                                );
                        }

                        if (inspectionItems.Contains(leadItemProvider.LeadContamination) || enforceAllChecks)
                        {
                            leadResult.LeadContamination = inspectLeadContamination(
                                leadRegion: leadRegion,
                                minSize: teaching.LeadContaminationMinSize,
                                maxSize: teaching.LeadContaminationMaxSize,
                                out HObject region
                                );

                            using (region)
                            {
                                if (leadResult.LeadContamination.Type != EResultType.Good)
                                {
                                    var regionTrans = region
                                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                        .DisposeBy(DisposeBag);

                                    renderData.ResultDrawings.Add((drawingObject: regionTrans, EResultType.LeadContamination.GetResultColor((InspectionTeaching)teaching)));
                                }
                            }
                        }

                        if (inspectionItems.Contains(leadItemProvider.LeadPitch) || enforceAllChecks)
                        {
                            leadResult.LeadPitch = inspectLeadPitches(fillUpPadRegion, teaching.LeadRois.ToList());
                        }

                        if (inspectionItems.Contains(leadItemProvider.LeadOffset) || enforceAllChecks)
                        {
                            leadResult.LeadOffset = inspectLeadOffset(fillUpPadRegion, teaching.LeadPxPoses.ToList(), out HObject region);

                            using (region)
                            {
                                if (leadResult.LeadOffset.Type != EResultType.Good)
                                {
                                    var regionTrans = region
                                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                        .DisposeBy(DisposeBag);

                                    renderData.ResultDrawings.Add((drawingObject: regionTrans, EResultType.LeadOffset.GetResultColor((InspectionTeaching)teaching)));
                                }
                            }
                        }

                        if (inspectionItems.Contains(leadItemProvider.LeadSize) || enforceAllChecks)
                        {
                            leadResult.LeadSize = inspectLeadSizes(fillUpPadRegion, teaching.LeadAverageSize);
                        }

                        if (inspectionItems.Contains(leadItemProvider.LeadArea) || enforceAllChecks)
                        {
                            leadResult.LeadArea = inspectLeadArea(fillUpPadRegion, teaching.LeadAverageArea);
                        }

                        if (inspectionItems.Contains(leadItemProvider.LeadPerimeter) || enforceAllChecks)
                        {
                            leadResult.LeadPerimeter = inspectLeadPerimeter(fillUpPadRegion, teaching.LeadAveragePerimeter, out HObject region);

                            using (region)
                            {
                                if (leadResult.LeadPerimeter.Type != EResultType.Good)
                                {
                                    var regionTrans = region
                                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                        .DisposeBy(DisposeBag);

                                    renderData.ResultDrawings.Add((drawingObject: regionTrans, EResultType.LeadPerimeter.GetResultColor((InspectionTeaching)teaching)));
                                }
                            }
                        }
                    }

                    return leadResult;
                }
            }
        }
    }

    partial class LeadTeachingService<TTeaching, TResult, TItem> : ILeadTeachingService<TTeaching, TResult, TItem>
    {

        private void findLeads(HObject image, List<Roi> rois, Threshold threshold, out HObject region)
        {
            if (typeof(TTeaching).IsAssignableFrom(typeof(LgaTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridLgaTeaching)))
                LgaEngine.FindLeads(image, rois, threshold, out region);
            else if (typeof(TTeaching).IsAssignableFrom(typeof(QfnTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridQfnTeaching)))
                QfnEngine.FindLeads(image, rois, threshold, out region);
            else
                throw new NotSupportedException($"findLeads is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<List<LengthStatsInRoi>> inspectLeadPitches(HObject leadsRegion, List<Roi> leadRois)
        {
            if (typeof(TTeaching).IsAssignableFrom(typeof(LgaTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridLgaTeaching)))
                return LgaEngine.InspectLeadPitches(leadsRegion, leadRois);
            else if (typeof(TTeaching).IsAssignableFrom(typeof(QfnTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridQfnTeaching)))
                return QfnEngine.InspectLeadPitches(leadsRegion, leadRois);
            else
                throw new NotSupportedException($"inspectLeadPitches is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Size>> inspectLeadSizes(HObject leadsRegion, Size originalSize)
        {
            if (typeof(TTeaching).IsAssignableFrom(typeof(LgaTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridLgaTeaching)))
                return LgaEngine.InspectLeadSizes(leadsRegion, originalSize);
            else if (typeof(TTeaching).IsAssignableFrom(typeof(QfnTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridQfnTeaching)))
                return QfnEngine.InspectLeadSizes(leadsRegion, originalSize);
            else
                throw new NotSupportedException($"inspectLeadSizes is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Length>> inspectLeadPerimeter(HObject region, double avgPerimeter, out HObject errorRegion)
        {
            if (typeof(TTeaching).IsAssignableFrom(typeof(LgaTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridLgaTeaching)))
                return LgaEngine.InspectLeadPerimeter(region, avgPerimeter, out errorRegion);
            else if (typeof(TTeaching).IsAssignableFrom(typeof(QfnTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridQfnTeaching)))
                return QfnEngine.InspectLeadPerimeter(region, avgPerimeter, out errorRegion);
            else
                throw new NotSupportedException($"inspectLeadPerimeter is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<int> inspectLeadContamination(HObject leadRegion, int minSize, int maxSize, out HObject errorRegion)
        {
            if (typeof(TTeaching).IsAssignableFrom(typeof(LgaTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridLgaTeaching)))
                return LgaEngine.InspectLeadContamination(leadRegion, minSize, maxSize, out errorRegion);
            else if (typeof(TTeaching).IsAssignableFrom(typeof(QfnTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridQfnTeaching)))
                return QfnEngine.InspectLeadContamination(leadRegion, minSize, maxSize, out errorRegion);
            else
                throw new NotSupportedException($"inspectLeadContamination is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Ratio>> inspectLeadArea(HObject region, int leadAvgArea)
        {
            if (typeof(TTeaching).IsAssignableFrom(typeof(LgaTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridLgaTeaching)))
                return LgaEngine.InspectLeadArea(region, leadAvgArea);
            else if (typeof(TTeaching).IsAssignableFrom(typeof(QfnTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridQfnTeaching)))
                return QfnEngine.InspectLeadArea(region, leadAvgArea);
            else
                throw new NotSupportedException($"inspectLeadArea is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Pose>> inspectLeadOffset(HObject region, List<Pose> centerPxPoses, out HObject errorRegion)
        {
            if (typeof(TTeaching).IsAssignableFrom(typeof(LgaTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridLgaTeaching)))
                return LgaEngine.InspectLeadOffset(region, centerPxPoses, out errorRegion);
            else if (typeof(TTeaching).IsAssignableFrom(typeof(QfnTeaching))
                || typeof(TTeaching).IsAssignableFrom(typeof(GridQfnTeaching)))
                return QfnEngine.InspectLeadOffset(region, centerPxPoses, out errorRegion);
            else
                throw new NotSupportedException($"inspectLeadOffset is not supported for teaching type: {typeof(TTeaching).Name}");
        }
    }
}
