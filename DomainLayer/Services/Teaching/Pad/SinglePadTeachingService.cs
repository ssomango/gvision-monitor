using GVisionWpf.DomainLayer.Align;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Pad;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Pad;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data;

namespace GVisionWpf.DomainLayer.Services.Teaching.Pad
{
    public sealed partial class SinglePadTeachingService<TTeaching, TResult, TItem> : ISinglePadTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private ISinglePadItemProvider<TItem>? singlePadItemProvider = SinglePadItemProviderFactory.GetProvider<TItem>();
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
                && teaching is IRejectMarkTeachingModel<TTeaching> {RejectMarkRoi : not null } rejectMarkTeaching)
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

            return inspectionImage;
        }

        public ISinglePadTeachingModel<TTeaching> TeachPad(HObject teachingImage, HObject packageRegion, ECamera camera, ISinglePadTeachingModel<TTeaching> teaching, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.PadRoi);

            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

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
         

            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeahcing &&
                dontCareTeahcing.DontCareRois.Count > 0)
            {
                VisionOperation.ReduceDomainComplement(inspectionImage, dontCareTeahcing.DontCareRois.ToList(), out inspectionImage);
            }

            findPad(inspectionImage, teaching.PadRoi, teaching.PadThreshold, out HObject padRegion);

            using (padRegion)
            {
                VisionOperation.GetAverageArea(padRegion, out int padPxArea);
                inspectedTeaching.PadArea = padPxArea;

                VisionOperation.GetRegionOrientationOfSmallestRectangle2(padRegion, out _, out Size padPxSize);
                inspectedTeaching.PadSize = padPxSize;
                
                return inspectedTeaching;
            }
        }

        public ISinglePadInspectionResultModel<TResult> InspectPad(AlignContext alignContext, ISinglePadTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.PadRoi);

            ISinglePadInspectionResultModel<TResult> padResult = (ISinglePadInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            HObject inspectionImage = preprocessInspectionImage(
                alignContext: alignContext,
                inspectionItems: inspectionItems,
                teaching: (TTeaching)teaching
                );

            findPad(inspectionImage, teaching.PadRoi, teaching.PadThreshold, out HObject padRegion);

            HObject padRegionTrans = padRegion 
                .AffineTransformRegion(alignContext.TransformMatrixInvert)
                .DisposeBy(DisposeBag);

            renderData.ResultDrawings.Add((drawingObject: padRegionTrans, EColor.Green));

            if (singlePadItemProvider is not null)
            {
                if (inspectionItems.Contains(singlePadItemProvider.PadSize) || enforceAllChecks)
                {
                    padResult.PadSize = inspectPadSize(padRegion, teaching.PadSize);
                }

                if (inspectionItems.Contains(singlePadItemProvider.PadArea) || enforceAllChecks)
                {
                    padResult.PadArea = inspectPadArea(padRegion, teaching.PadArea);
                }
            }

            return padResult;
        }
    }

    partial class SinglePadTeachingService<TTeaching, TResult, TItem>
    {
        private void findPad(HObject image, Roi roi, Threshold threshold, out HObject region)
        {
            if (typeof(QfnTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridQfnTeaching).IsAssignableFrom(typeof(TTeaching)))
                QfnEngine.FindPad(image, roi, threshold, out region);
            else
                throw new NotSupportedException($"findPad is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<Size> inspectPadSize(HObject padRegion, Size originalSize)
        {
            if (typeof(QfnTeaching).IsAssignableFrom(typeof(TTeaching))
                     || typeof(GridQfnTeaching).IsAssignableFrom(typeof(TTeaching)))
                return QfnEngine.InspectPadSize(padRegion, originalSize);
            else
                throw new NotSupportedException($"inspectPadSize is not supported for teaching type: {typeof(TTeaching).Name}");
        }

        private Result<Ratio> inspectPadArea(HObject padRegion, int padArea)
        {
            if (typeof(QfnTeaching).IsAssignableFrom(typeof(TTeaching))
                || typeof(GridQfnTeaching).IsAssignableFrom(typeof(TTeaching)))
                return QfnEngine.InspectPadArea(padRegion, padArea);
            else
                throw new NotSupportedException($"inspectPadSize is not supported for teaching type: {typeof(TTeaching).Name}");
        }
    }
}
