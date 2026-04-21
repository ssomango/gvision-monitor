using System.Threading;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Surface;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.Surface
{
    public sealed partial class MapSurfaceTeachingService<TTeaching, TResult, TItem> : ISurfaceTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private ISawingItemProvider<TItem>? sawItemProvider = SawingItemProviderFactory.GetProvider<TItem>();
        private ISurfaceItemProvider<TItem>? surfaceItemProvider = SurfaceItemProviderFactory.GetProvider<TItem>();

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        private HObject? scratchRegion;
        private HObject? foreignMaterialRegion;
        private HObject? contaminationRegion;

        public void Dispose() => DisposeBag.Dispose();

        private HObject preprocessInspecitonImage(AlignContext alignContext, TTeaching teaching, HashSet<TItem> inspectionItems)
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

            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching && !dontCareTeaching.DontCareRois.IsNullOrEmpty())
            {
                inspectionImage = inspectionImage.OmitRegionFromTarget(dontCareTeaching.DontCareRois.ToList(), 2);
            }
            
            if (teaching is IRejectMarkTeachingModel<TTeaching> { RejectMarkRoi : not null } rejectMarkModel)
            {
                VisionEngine.InspectRejectMark(inspectionImage, rejectMarkModel.RejectMarkRoi, rejectMarkModel.RejectMarkThreshold, rejectMarkModel.RejectMarkMinSize, rejectMarkModel.RejectMarkMaxSize, out HObject rejectMarkRegion);
                using (rejectMarkRegion)
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(rejectMarkRegion, 2);
                }
            }

            if (teaching is IDataCodeTeachingModel<TTeaching> { CodeRoi : not null } dataCodeModel)
            {
                using (var codeRegion = dataCodeModel.CodeRoi.Roi2Region())
                {
                    inspectionImage = inspectionImage.OmitRegionFromTarget(codeRegion, 2);
                }
            }

            if (teaching is IMarkTeachingModel<TTeaching> markModel && !markModel.MarkItems.IsNullOrEmpty())
            {
                foreach (var markItem in markModel.MarkItems)
                {
                    using (var markRegion = markItem.Roi.Roi2Region())
                    {
                        inspectionImage = inspectionImage.OmitRegionFromTarget(markRegion, 2);
                    }
                }
             
                //foreach (MarkItem mark in markModel.MarkItems)
                //{
                //    HOperatorSet.GenEmptyRegion(out HObject markRegions);

                //    using HObject markImage = inspectionImage
                //        .ReduceDomain(mark.Roi);

                //    HObject connectedTextRegion = VisionOperation.GetConnectedTextRegion(markImage, markModel.MarkThreshold);
                //    HOperatorSet.DilationCircle(mark.connectedTextRegion, out HObject dilatedRegions, 15);

                //    using (dilatedRegions)
                //    {
                //        HOperatorSet.Union2(markRegions, dilatedRegions, out markRegions);
                        
                //        using (markRegions)
                //        {
                //            VisionOperation.ReduceDomainComplement(inspectionImage, markRegions, out inspectionImage);
                //        }
                //    }
                //}
            }

            if (teaching is ISawingTeachingModel<TTeaching> sawModel)
            {
                if (sawItemProvider is not null && inspectionItems.Contains(sawItemProvider.SawOffset))
                {
                    if (teaching is IMarkTeachingModel<TTeaching> { MarkThreshold : var markThreshold})
                    {
                        HObject targetRegion = VisionOperation.GetConnectedTextRegion(inspectionImage, markThreshold);
                        VisionOperation.ReduceDomainComplement(inspectionImage, targetRegion, out inspectionImage);
                    }
                }
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
                var inspectionImage = preprocessInspecitonImage(alignContext, (TTeaching)teaching, inspectionItems);

                if (scratchRegion is not null)
                    inspectionImage = inspectionImage.OmitRegionFromTarget(scratchRegion, 2);

                if (foreignMaterialRegion is not null)
                    inspectionImage = inspectionImage.OmitRegionFromTarget(foreignMaterialRegion, 2);

                result.Contamination = VisionEngine.InspectContamination(
                    image: inspectionImage,
                    rois: teaching.SurfaceRois.ToList(),
                    threshold: teaching.ContaminationThreshold,
                    minSize: teaching.ContaminationMinSize,
                    maxSize: teaching.ContaminationMaxSize,
                    out HObject region);

                contaminationRegion = region.DisposeBy(DisposeBag);

                if (result.Contamination.Type != EResultType.Good)
                {
                    HObject contaminationRegionTrans = region
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    renderData.ResultDrawings.Add((drawingObject: contaminationRegionTrans, color: EResultType.Contamination.GetResultColor((InspectionTeaching)teaching)));
                }

                inspectionImage.Dispose();
            }

            return result;
        }

        public IForeignMaterialInspectionResultModel<TResult> InspectForeignMaterials(AlignContext alignContext, IForeignMaterialTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IForeignMaterialInspectionResultModel<TResult> result = (IForeignMaterialInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if ((surfaceItemProvider is not null && inspectionItems.Contains(surfaceItemProvider.ForeignMaterial))
                || enforceAllChecks)
            {
                var inspectionImage = preprocessInspecitonImage(alignContext, (TTeaching)teaching, inspectionItems);

                if (scratchRegion is not null)
                    inspectionImage = inspectionImage.OmitRegionFromTarget(scratchRegion, 2);

                if (contaminationRegion is not null)
                    inspectionImage = inspectionImage.OmitRegionFromTarget(contaminationRegion, 2);

                result.ForeignMaterial = VisionEngine.InspectForeignMaterial(
                    image: inspectionImage,
                    rois: teaching.SurfaceRois.ToList(),
                    threshold: teaching.ForeignMaterialThreshold,
                    minSize: teaching.ForeignMaterialMinSize,
                    maxSize: teaching.ForeignMaterialMaxSize,
                    out HObject region);

                foreignMaterialRegion = region.DisposeBy(DisposeBag);

                if (result.ForeignMaterial.Type != EResultType.Good)
                {
                    HObject foreignMaterialRegionTrans = region
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    renderData.ResultDrawings.Add((drawingObject: foreignMaterialRegionTrans, color: EResultType.ForeignMaterial.GetResultColor((InspectionTeaching)teaching)));
                }

                inspectionImage.Dispose();
            }

            return result;
        }

        public IScratchInspectionResultModel<TResult> InspectScratches(AlignContext alignContext, IScratchTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IScratchInspectionResultModel<TResult> result = (IScratchInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();

            if ((surfaceItemProvider is not null && inspectionItems.Contains(surfaceItemProvider.Scratch))
                || enforceAllChecks)
            {
                var inspectionImage = preprocessInspecitonImage(alignContext, (TTeaching)teaching, inspectionItems);

                if (foreignMaterialRegion is not null)
                    inspectionImage = inspectionImage.OmitRegionFromTarget(foreignMaterialRegion, 2);

                if (contaminationRegion is not null) 
                    inspectionImage = inspectionImage.OmitRegionFromTarget(contaminationRegion, 2);


                result.Scratch = VisionEngine.InspectScratch(
                    image: inspectionImage,
                    rois: teaching.SurfaceRois.ToList(),
                    threshold: teaching.ScratchThreshold,
                    minSize: teaching.ScratchMinSize,
                    maxSize: teaching.ScratchMaxSize,
                    out HObject region
                    );

                scratchRegion = region.DisposeBy(DisposeBag);

                if (result.Scratch.Type != EResultType.Good)
                {
                    HObject scratchRegionTrans = region
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    renderData.ResultDrawings.Add((drawingObject: scratchRegionTrans, color: EResultType.Scratch.GetResultColor((InspectionTeaching)teaching)));
                }

                inspectionImage.Dispose();
            }

            return result;
        }

        public void ResetInspectionState()
        {
            scratchRegion = null;
            foreignMaterialRegion = null;
            contaminationRegion = null;
        }
    }
}
