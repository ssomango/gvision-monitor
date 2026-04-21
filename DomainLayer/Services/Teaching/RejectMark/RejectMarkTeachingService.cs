using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.RejectMark;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.RejectMark
{
    public sealed partial class RejectMarkTeachingService<TTeaching, TResult, TItem> : IRejectMarkTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IRejectMarkItemProvider<TItem>? rejectMarkItemProvider = RejectMarkItemProviderFactory.GetProvider<TItem>();
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

            return inspectionImage;
        }

        public IRejectMarkInspectionResultModel<TResult> InspectRejectMark(AlignContext alignContext, IRejectMarkTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IRejectMarkInspectionResultModel<TResult> result = (IRejectMarkInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if (((rejectMarkItemProvider is not null && inspectionItems.Contains(rejectMarkItemProvider.RejectMark)) || enforceAllChecks)
                && teaching.RejectMarkRoi is not null)
            {
                var inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

                result.RejectMark = VisionEngine.InspectRejectMark(
                    image: inspectionImage,
                    roi: teaching.RejectMarkRoi,
                    threshold: teaching.RejectMarkThreshold,
                    minSize: teaching.RejectMarkMinSize,
                    maxSize: teaching.RejectMarkMaxSize,
                    out HObject rejectMarkRegion
                    );

                using (rejectMarkRegion)
                {
                    if (result.RejectMark.Type != EResultType.Good)
                    {
                        var rejectMarkRegionTrans = rejectMarkRegion
                            .AffineTransformRegion(alignContext.TransformMatrixInvert)
                            .DisposeBy(DisposeBag);

                        FixedText text = new FixedText("Result : " + EResultType.RejectMark.ToString().ToUpper(), 1, EColor.Red);
                        renderData.AddText(text);

                        renderData.ResultDrawings.Add((drawingObject: rejectMarkRegionTrans, color: result.RejectMark.Type.GetResultColor((InspectionTeaching)teaching)));
                    }
                }
            }

            return result;
        }
    }
}
