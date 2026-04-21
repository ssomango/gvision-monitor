using GVisionWpf.DomainLayer.Align;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.Sawing
{
    public sealed partial class MapSawingTeachingService<TTeaching, TResult, TItem> : ISawingTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private ISawingItemProvider<TItem>? sawingItemProvider = SawingItemProviderFactory.GetProvider<TItem>();

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

            return inspectionImage;
        }

        public ISawingTeachingModel<TTeaching> TeachSawOffset(HObject teachingImage, AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            if (!inspectionItems.Contains(sawingItemProvider.SawOffset)) return inspectedTeaching;

            HOperatorSet.GenEmptyRegion(out HObject blobRegion);

            if (teaching is IMarkTeachingModel<TTeaching> markTeaching)
            {
                blobRegion = VisionOperation
                    .GetConnectedTextRegion(teachingImage, markTeaching.MarkThreshold)
                    .DisposeBy(DisposeBag);
            }

            foreach (var sawOffsetItem in inspectedTeaching.SawOffsetItems)
            {
                HOperatorSet.SelectRegionPoint(blobRegion, out HObject targetRegion, sawOffsetItem.SawOffsetTargetPoint.Row, sawOffsetItem.SawOffsetTargetPoint.Col);

                using (targetRegion)
                {
                    foreach (EDirection direction in sawOffsetItem.Directions)
                    {
                        VisionOperation.GetLineFromPackagePoints(alignContext.PackagePoints, direction, out Point start, out Point end);
                        VisionOperation.Distance(start, end, targetRegion, out Length pxDistance, out _);
                        sawOffsetItem.TaughtDistances[direction] = pxDistance.ConvertFromPixel(camera).Value;
                    }
                }
            }

            return inspectedTeaching;
        }

        public IBurrInspectionResultModel<TResult> InspectBurr(AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            throw new NotImplementedException();
        }

        public ICornerDegreeInspectionResultModel<TResult> InspectCornerDegree(AlignContext alignContext, double tolerance, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ICornerDegreeInspectionResultModel<TResult> result = (ICornerDegreeInspectionResultModel<TResult>)new TResult();

            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.CornerDegree)) || enforceAllChecks)
            {
                result.CornerDegree = MapEngine.InspectCornerDegree(alignContext.PackagePoints, out _, out _);
            }

            return result;
        }

        public IChippingInspectionResultModel<TResult> InspectChipping(AlignContext alignContext, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            // INTENTION: MapInspection에서 Chipping은 원본 이미지를 그대로 사용하고 있어 Align 생략

            IChippingInspectionResultModel<TResult> result = (IChippingInspectionResultModel<TResult>)new TResult();

            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.Chipping)) || enforceAllChecks)
            {
                HOperatorSet.GenEmptyRegion(out HObject dontCareRegion);
                dontCareRegion.DisposeBy(DisposeBag);

                if (teaching is IMarkTeachingModel<MoldTeaching> markModel)
                {
                    foreach (MarkItem mark in markModel.MarkItems)
                    {
                        HOperatorSet.DilationCircle(mark.connectedTextRegion, out HObject dilatedRegions, 7);

                        HOperatorSet.Union2(dontCareRegion, dilatedRegions, out dontCareRegion);

                        dilatedRegions.Dispose();
                    }
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
                    cameraType: ECamera.Mapping,
                    out HObject region
                    );

                region.DisposeBy(DisposeBag);

                renderData.ResultDrawings.Add((drawingObject: region, EResultType.Chipping.GetResultColor((InspectionTeaching)teaching)));
            }

            return result;
        }

        public ISawOffsetInspectionResultModel<TResult> InspectSawOffset(AlignContext alignContext, double xTolerance, double yTolerance, ECamera camera, ISawingTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ISawOffsetInspectionResultModel<TResult> result = (ISawOffsetInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if ((sawingItemProvider is not null && inspectionItems.Contains(sawingItemProvider.SawOffset)) || enforceAllChecks)
            {
                var inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching, inspectionItems);

                if (teaching is IMarkTeachingModel<MoldTeaching> { MarkThreshold : var markThreshold })
                {
                    HObject targetRegion = VisionOperation.GetConnectedTextRegion(inspectionImage, markThreshold);
                    VisionOperation.AffineTransformPoints(alignContext.PackagePoints, alignContext.TransformMatrix, out List<Point> aligendPackagePoints);
                    result.SawOffset = MapEngine.InspectNewSawOffset(teaching.SawOffsetItems.First(), aligendPackagePoints, targetRegion, targetRegion, out HObject footOfPerpendicular);

                    footOfPerpendicular.DisposeBy(DisposeBag);
                    targetRegion.DisposeBy(DisposeBag);
                    inspectionImage.DisposeBy(DisposeBag);

                }
            }

            return result;
        }
    }
}
