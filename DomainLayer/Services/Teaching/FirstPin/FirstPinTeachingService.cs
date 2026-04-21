using GVisionWpf.DomainLayer.Align;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.FirstPin;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.FirstPin
{
    public sealed class FirstPinTeachingService<TTeaching, TResult, TItem> : IFirstPinTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
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
            if (rejectMarkItemProvider is not null && inspectionItems.Contains(rejectMarkItemProvider.RejectMark) &&
                teaching is IRejectMarkTeachingModel<TTeaching> { RejectMarkRoi : not null } rejectMarkTeaching)
            {
                VisionEngine.InspectRejectMark(inspectionImage, rejectMarkTeaching.RejectMarkRoi, rejectMarkTeaching.RejectMarkThreshold, rejectMarkTeaching.RejectMarkMinSize, rejectMarkTeaching.RejectMarkMaxSize, out HObject rejectMarkRegion);
                using (rejectMarkRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(rejectMarkRegion, 2);
                }
            }

            return inspectionImage;
        }


        public IFirstPinTeachingModel<TTeaching> TeachFirstPin(HObject teachingImage, IFirstPinTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.FirstPinRoi);

            renderData = new InspectionRenderData();
            var inspecteadTeaching = DeepCopy.Copy(teaching);

            VisionOperation.FindRect(teachingImage, teaching.FirstPinRoi, teaching.FirstPinThreshold, out Rect firstPinRect);

            HObject firstPinRegion = new Roi(firstPinRect)
                .Roi2Region()
                .DisposeBy(DisposeBag);

            inspecteadTeaching.FirstPinRect = firstPinRect;

            renderData.ResultDrawings.Add((drawingObject: firstPinRegion, color: EColor.Green));

            return inspecteadTeaching;
        }

        public IFirstPinInspectResultModel<TResult> InspectFirstPin(AlignContext alignContext, IFirstPinTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IFirstPinInspectResultModel<TResult> result = (IFirstPinInspectResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            var inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

            if (((firstPinItemProvider is not null && inspectionItems.Contains(firstPinItemProvider.FirstPin) || enforceAllChecks))
                && teaching.FirstPinRoi is not null)
            {
                result.FirstPin = VisionEngine.InspectFirstPin(
                    image: inspectionImage,
                    roi: teaching.FirstPinRoi,
                    threshold: teaching.FirstPinThreshold,
                    type: teaching.FirstPinType,
                    out HObject firstPinRegion
                    );

                using (firstPinRegion)
                {
                    renderData.ResultDrawings.Add((drawingObject: firstPinRegion, color: EResultType.FirstPin.GetResultColor((InspectionTeaching)teaching)));
                }
            }

            return result;
        }
    }
}
