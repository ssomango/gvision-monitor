using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.Interfaces.MultiPad;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.MultiPad;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data;

namespace GVisionWpf.DomainLayer.Services.Teaching.MultiPad
{
    public sealed partial class MultiPadTeachingService<TTeaching, TResult, TItem> : IMultiPadTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IMultiPadItemProvider<TItem>? multiPadItemProvider = MultiPadItemProviderFactory.GetProvider<TItem>();
        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
        private IFirstPinItemProvider<TItem>? firstPinItemProvider = FirstPinItemProviderFactory.GetProvider<TItem>();

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
                && teaching is IFirstPinTeachingModel<TTeaching> {FirstPinRoi : not null } firstPinTeaching)
            {
                VisionEngine.InspectFirstPin(inspectionImage, firstPinTeaching.FirstPinRoi, firstPinTeaching.FirstPinThreshold, firstPinTeaching.FirstPinType, out HObject firstPinRegion);
                using (firstPinRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(firstPinRegion, 2);
                }
            }

            return inspectionImage;
        }

        public IMultiPadTeachingModel<TTeaching> TeachPads(HObject teachingImage, HObject packageRegion, ECamera camera, IMultiPadTeachingModel<TTeaching> teaching, out InspectionRenderData renderData)
        {
            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            HOperatorSet.ErosionCircle(packageRegion, out packageRegion, 2);
            VisionOperation.ReduceDomain(teachingImage, packageRegion, out HObject image);

            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeahcing &&
                dontCareTeahcing.DontCareRois.Count > 0)
            {
                VisionOperation.ReduceDomainComplement(image, dontCareTeahcing.DontCareRois.ToList(), out image);
            }

            findMultiPad(image, teaching.PadRois.ToList(), teaching.MultiPadThreshold, out HObject multiPadRegion);

            image.Dispose();

            using (multiPadRegion)
            {
                VisionOperation.GetRegionOrientationOfSmallestRectangle2(multiPadRegion, out List<Pose> PadPxPoses, out _);
                inspectedTeaching.PadPxPoses = PadPxPoses;

                VisionOperation.GetAverageArea(multiPadRegion, out int padAvgArea);
                inspectedTeaching.MultiPadAvgArea = padAvgArea;

                #region padPithces
                var padPitches = inspectMultiPadPitches(
                    multiPadRegion: multiPadRegion,
                    padRois: teaching.PadRois.ToList(),
                    cameraType: camera
                    ).Value;

                inspectedTeaching.MultiPadAvgPitch = padPitches?.Select(pitches => pitches.MemberwiseAverage().Value).Average() ?? 0;
                #endregion

                #region padSizes
                var padSizes = inspectPadSizes(
                    padRegion: multiPadRegion,
                    originalSize: new Size(0, 0)
                    ).Value;

                inspectedTeaching.MultiPadSizes = padSizes;

                inspectedTeaching.MultiPadAvgSize = new Size(
                    width: padSizes?.MemberwiseAverage().Width ?? 0,
                    height: padSizes?.MemberwiseAverage().Height ?? 0);

                #endregion

                #region padPerimeter
                var padPerimeter = inspectPadPerimeter(
                    region: multiPadRegion,
                    avgPerimeter: 0,
                    out _
                    ).Value;

                inspectedTeaching.MultiPadAvgPerimeter = padPerimeter?.MemberwiseAverage().Value ?? 0;
                #endregion

                HOperatorSet.FillUp(multiPadRegion, out HObject fillUpPadRegion);
                fillUpPadRegion.DisposeBy(DisposeBag);

                renderData.ResultDrawings.Add((drawingObject: fillUpPadRegion, EColor.Green));

                return inspectedTeaching;
            }
        }

        public IMultiPadInspectionResultModel<TResult> InspectPads(AlignContext alignContext, IMultiPadTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IMultiPadInspectionResultModel<TResult> padResult = (IMultiPadInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            HObject inspectionImage = preprocessInspectionImage(
                alignContext: alignContext,
                teaching: (TTeaching)teaching,
                inspectionItems: inspectionItems
                );

            findMultiPad(inspectionImage, teaching.PadRois.ToList(), teaching.MultiPadThreshold, out HObject padRegion);
            inspectionImage = inspectionImage.OmitRegionFromTarget(padRegion, 2);

            HOperatorSet.FillUp(padRegion, out HObject fillUpPadRegion);

            HObject fillUpPadRegionTrans = fillUpPadRegion
                .AffineTransformRegion(alignContext.TransformMatrixInvert)
                .DisposeBy(DisposeBag);

            renderData.ResultDrawings.Add((drawingObject: fillUpPadRegionTrans, EColor.Green));

            using (fillUpPadRegion)
            {
                if (multiPadItemProvider is not null)
                {
                    if (inspectionItems.Contains(multiPadItemProvider.MultiPadCount) || enforceAllChecks)
                    {
                        int multiPadCount = VisionOperation.GetCountOf(fillUpPadRegion);

                        padResult.MultiPadCount = new Result<int>(
                            type: multiPadCount == teaching.PadPxPoses.Count() ? EResultType.Good : EResultType.MultiPadCount,
                            value: multiPadCount
                            );
                    }

                    if (inspectionItems.Contains(multiPadItemProvider.MultiPadSize) || enforceAllChecks)
                    {
                        padResult.MultiPadSize = inspectPadSizes(fillUpPadRegion, teaching.MultiPadAvgSize);
                    }

                    if (inspectionItems.Contains(multiPadItemProvider.MultiPadArea) || enforceAllChecks)
                    {
                        padResult.MultiPadArea = inspectMultiPadArea(fillUpPadRegion, teaching.MultiPadAvgArea);
                    }

                    if (inspectionItems.Contains(multiPadItemProvider.MultiPadPitch) || enforceAllChecks)
                    {
                        padResult.MultiPadPitch = inspectMultiPadPitches(fillUpPadRegion, teaching.PadRois.ToList(), ECamera.PRS);
                    }

                    if (inspectionItems.Contains(multiPadItemProvider.MultiPadPerimeter) || enforceAllChecks)
                    {
                        padResult.MultiPadPerimeter = inspectPadPerimeter(fillUpPadRegion, teaching.MultiPadAvgPerimeter, out HObject region);

                        if (padResult.MultiPadPerimeter.Type != EResultType.Good)
                        {
                            HObject regionTrans = region
                                .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                .DisposeBy(DisposeBag);

                            renderData.ResultDrawings.Add((drawingObject: regionTrans, EResultType.MultiPadPerimeter.GetResultColor((InspectionTeaching)teaching)));
                        }
                    }

                    if (inspectionItems.Contains(multiPadItemProvider.MultiPadOffset) || enforceAllChecks)
                    {
                        padResult.MultiPadOffset = inspectMultiPadOffset(fillUpPadRegion, teaching.PadPxPoses.ToList(), out HObject region);

                        if (padResult.MultiPadOffset.Type != EResultType.Good)
                        {
                            HObject regionTrans = region
                                .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                .DisposeBy(DisposeBag);

                            renderData.ResultDrawings.Add((drawingObject: regionTrans, color: EResultType.MultiPadOffset.GetResultColor((InspectionTeaching)teaching)));
                        }
                    }

                    if (inspectionItems.Contains(multiPadItemProvider.MultiPadContamination) || enforceAllChecks)
                    {
                        padResult.MultiPadContamination = inspectMultiPadContamination(padRegion, teaching.PadContaminationMinSize, teaching.PadContaminationMaxSize, out HObject region);
                        if (padResult.MultiPadContamination.Type != EResultType.Good)
                        {
                            HObject regionTrans = region
                                .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                .DisposeBy(DisposeBag);

                            renderData.ResultDrawings.Add((drawingObject: regionTrans, color: EResultType.MultiPadContamination.GetResultColor((InspectionTeaching)teaching)));
                        }
                    }
                }

                return padResult;
            }
        }
    }

    partial class MultiPadTeachingService<TTeaching, TResult, TItem>
    {
        private void findMultiPad(HObject image, List<Roi> rois, Threshold threshold, out HObject region)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching)) 
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                LgaEngine.FindMultiPad(image, rois, threshold, out region);
            else
                throw new NotSupportedException($"findMultiPad is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Size>> inspectPadSizes(HObject padRegion, Size originalSize)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectPadSizes(padRegion, originalSize);
            else
                throw new NotSupportedException($"inspectPadSizes is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<List<LengthStatsInRoi>> inspectMultiPadPitches(HObject multiPadRegion, List<Roi> padRois, ECamera cameraType)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectMultiPadPitches(multiPadRegion, padRois, cameraType);
            else
                throw new NotSupportedException($"inspectMultiPadPitches is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Ratio>> inspectMultiPadArea(HObject region, int multiPadAvgArea)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectMultiPadArea(region, multiPadAvgArea);
            else
                throw new NotSupportedException($"inspectMultiPadArea is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Length>> inspectPadPerimeter(HObject region, double avgPerimeter, out HObject errorRegion)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectPadPerimeter(region, avgPerimeter, out errorRegion);
            else
                throw new NotSupportedException($"inspectPadPerimeter is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<StatisticalList<Pose>> inspectMultiPadOffset(HObject region, List<Pose> centerPxPoses, out HObject errorRegion)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectMultiPadOffset(region, centerPxPoses, out errorRegion);
            else
                throw new NotSupportedException($"inspectMultiPadOffset is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<int> inspectMultiPadContamination(HObject padRegion, int minSize, int maxSize, out HObject errorRegion)
        {
            if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridLgaTeaching).IsAssignableFrom(typeof(TTeaching)))
                return LgaEngine.InspectMultiPadContamination(padRegion, minSize, maxSize, out errorRegion);
            else
                throw new NotSupportedException($"inspectMultiPadContamination is not supported for teaching type: {typeof(TTeaching).Name}");
        }
    }
}
