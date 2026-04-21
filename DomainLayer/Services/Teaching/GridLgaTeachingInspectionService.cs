using System.Diagnostics;
using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching.DataCode;
using GVisionWpf.DomainLayer.Services.Teaching.FirstPin;
using GVisionWpf.DomainLayer.Services.Teaching.Lead;
using GVisionWpf.DomainLayer.Services.Teaching.Mark;
using GVisionWpf.DomainLayer.Services.Teaching.MultiPad;
using GVisionWpf.DomainLayer.Services.Teaching.Package;
using GVisionWpf.DomainLayer.Services.Teaching.Pad;
using GVisionWpf.DomainLayer.Services.Teaching.Pattern;
using GVisionWpf.DomainLayer.Services.Teaching.RejectMark;
using GVisionWpf.DomainLayer.Services.Teaching.Sawing;
using GVisionWpf.DomainLayer.Services.Teaching.Surface;
using GVisionWpf.Interfaces.Teaching.Ball;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.GlobalStates;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public sealed partial class GridLgaTeachingInspectionService : ITeachingInspectionService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, GridLgaTeaching teaching, ECamera camera, HashSet<LgaInspectionItem> inspectionItems)
        {
            ArgumentNullException.ThrowIfNull(GridPackageTeachingService);

            ConcurrentBag<RenderableInspectionResult> results = new ConcurrentBag<RenderableInspectionResult>();

            List<HObject> rotatedShots = images
                .Select(image => VisionOperation.RotateImage(image, teaching.RotateAngle))
                .ToList();

            HObject resultImage = rotatedShots.First()
                .CopyImage()
                .DisposeBy(DisposeBag);

            for (int i = 1; i < rotatedShots.Count(); i++)
            {
                HOperatorSet.AddImage(resultImage, rotatedShots[i], out resultImage, 0.5, 0);
            }

            List<Roi> packageRois = GridPackageTeachingService.PartitionRoi(teaching).ToList();

            await Parallel.ForEachAsync(packageRois.Select((packageRoi, index) => (packageRoi, index)), async (pair, cancellationToken) =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                LgaInspectionResult result = new LgaInspectionResult
                {
                    PackageNoInFov = pair.index + 1,
                    Shots = images,
                    Image = resultImage,
                    StartTime = DateTime.Now,
                    Type = EInspection.Mapping,
                }
                .DisposeBy(DisposeBag);

                AlignContext alignContext = new AlignContext();

                InspectionRenderData renderData = new InspectionRenderData();

                try
                {
                    if (GridPackageTeachingService != null)
                    {
                        using HObject imageForPackage = getInspectionImage(teaching, rotatedShots, LgaInspectionItem.PackageOffset);

                        GridPackageTeachingService.InspectSinglePackage(
                            image: imageForPackage,
                            teaching: teaching,
                            packageRoi: pair.packageRoi,
                            packageIndex: pair.index,
                            camera: camera,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData packageRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        alignContext.PackageRegion = result.PackageRegion;
                        alignContext.PackagePoints = result.PackagePoints;

                        renderData.MergeWith(packageRender);

                        if (result.HasDevice.Type == EResultType.NoDevice)
                            return;
                    }

                    ArgumentNullException.ThrowIfNull(result.PackageRegion);

                    List<HObject> inspectionShots = new List<HObject>();

                    foreach (var shot in rotatedShots)
                    {
                        MapEngine.AlignImageWithPose(
                            shot,
                            result.PackageRegion,
                            teaching.PackageCenter,
                            out HObject tmpAlignedImage,
                            out alignContext.TransformMatrix,
                            out alignContext.TransformMatrixInvert,
                            out _
                            );

                        VisionOperation.AffineTransformRegion(result.PackageRegion, alignContext.TransformMatrix, out HObject tmpAlignedPackageRegion);
                        HOperatorSet.ErosionCircle(tmpAlignedPackageRegion, out HObject erodedPackageRegion, 10);
                        VisionOperation.ReduceDomain(tmpAlignedImage, erodedPackageRegion, out HObject inspectionImage);
                        inspectionShots.Add(inspectionImage);
                    }


                    if (RejectMarkTeachingService != null)
                    {
                        using HObject imageForRejectMark = getInspectionImage(teaching, inspectionShots, LgaInspectionItem.RejectMark);
                        alignContext.AlignedImage = imageForRejectMark;

                        RejectMarkTeachingService.InspectRejectMark(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData rejectMarkRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        if (result.RejectMark.Type != EResultType.Good)
                        {
                            renderData.MergeWith(rejectMarkRender);
                            return;
                        }
                    }

                    if (FirstPinTeachingService != null)
                    {
                        using HObject imageForFirstPin = getInspectionImage(teaching, inspectionShots, LgaInspectionItem.PackageSize);
                        alignContext.AlignedImage = imageForFirstPin;

                        FirstPinTeachingService.InspectFirstPin(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData firstPinRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(firstPinRender);
                    }

                    if (MultiPadTeachingService != null)
                    {
                        using HObject imageForMultiPad = getInspectionImage(teaching, inspectionShots, LgaInspectionItem.MultiPadCount);
                        alignContext.AlignedImage = imageForMultiPad;

                        MultiPadTeachingService.InspectPads(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData padRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(padRender);
                    }

                    if (LeadTeachingService != null)
                    {
                        using HObject imageForLead = getInspectionImage(teaching, inspectionShots, LgaInspectionItem.LeadCount);
                        alignContext.AlignedImage = imageForLead;

                        LeadTeachingService.InspectLeads(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData leadRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(leadRender);
                    }

                    if (SurfaceTeachingService != null)
                    {
                        using HObject imageForSurface = getInspectionImage(teaching, inspectionShots, LgaInspectionItem.Scratch);
                        alignContext.AlignedImage = imageForSurface;
                        var surfaceService = new SurfaceTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);

                        surfaceService.InspectScratches(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData surfaceRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(surfaceRender);

                        surfaceService.InspectForeignMaterials(
                           alignContext: alignContext,
                           teaching: teaching,
                           enforceAllChecks: false,
                           inspectionItems: inspectionItems,
                           out InspectionRenderData foreignMaterialRender
                           )
                           .MergeTo(result)
                           .DisposeBy(DisposeBag);

                        renderData.MergeWith(foreignMaterialRender);


                        surfaceService.InspectContaminations(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData contaminationRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(contaminationRender);
                    }

                    if (SawingTeachingService != null)
                    {
                        SawingTeachingService.InspectCornerDegree(
                            alignContext: alignContext,
                            tolerance: GlobalSetting.Instance.Inspection.Tolerance.LgaCornerDegree,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out _
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        using HObject imageForSawOffset = getInspectionImage(teaching, inspectionShots, LgaInspectionItem.SawOffset);
                        alignContext.AlignedImage = imageForSawOffset;

                        SawingTeachingService.InspectSawOffset(
                            alignContext: alignContext,
                            xTolerance: GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetX,
                            yTolerance: GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetY,
                            camera: ECamera.Mapping,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out _
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        using HObject imageForChipping = getInspectionImage(teaching, rotatedShots, LgaInspectionItem.Chipping);
                        alignContext.AlignedImage = imageForChipping;

                        SawingTeachingService.InspectChipping(
                            alignContext: alignContext,
                            camera: ECamera.Mapping,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData chippingRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(chippingRender);

                        SawingTeachingService.InspectBurr(
                            alignContext: alignContext,
                            camera: ECamera.Mapping,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData burrRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(burrRender);
                    }
                }
                finally
                {
                    result.Duration = stopwatch.ElapsedMilliseconds;
                    stopwatch.Stop();

                    results.Add(new RenderableInspectionResult(result, renderData));
                }
            });

            var orderedResults = results.OrderBy(r => r.InspectionResult.PackageNoInFov).ToList();

            int nGoodDevices = orderedResults.Count(result => result.InspectionResult.EvaluateResults());
            int nBadDevices = orderedResults.Count(result => !result.InspectionResult.EvaluateResults());

            FixedText totalText = new FixedText($"TOTAL: {nGoodDevices + nBadDevices}, GOOD: {nGoodDevices}, BAD: {nBadDevices}", 1, nBadDevices > 0 ? EColor.Red : EColor.Green);
            orderedResults.ForEach(r => r.RenderData.AddText(totalText));

            return results;
        }

        public IGridPackageTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? GridPackageTeachingService { get; set; }

        public IFirstPinTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? FirstPinTeachingService { get; set; }

        public IMultiPadTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? MultiPadTeachingService { get; set; }

        public ILeadTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? LeadTeachingService { get; set; }

        public ISurfaceTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? SurfaceTeachingService { get; set; }

        public ISawingTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? SawingTeachingService { get; set; }

        public IRejectMarkTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? RejectMarkTeachingService { get; set; }

        #region unused
        public ISinglePackageTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? SinglePackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDataCodeTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? DataCodeTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMarkTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? MarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IBallTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? BallTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISinglePadTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? SinglePadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPatternTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>? PatternTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        public void Dispose() => DisposeBag.Dispose();

    
        public GridLgaTeachingInspectionService()
        {
            GridPackageTeachingService = new GridPackageTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            FirstPinTeachingService = new FirstPinTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            MultiPadTeachingService = new MultiPadTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            LeadTeachingService = new LeadTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            SurfaceTeachingService = new SurfaceTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new LeadAndPadSawingTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<GridLgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
        }

        ~GridLgaTeachingInspectionService() => Dispose();
    }

    partial class GridLgaTeachingInspectionService
    {
        private HObject getInspectionImage(GridLgaTeaching teaching, List<HObject> shots, LgaInspectionItem item)
        {
            teaching.ShotNumberForInspection.TryGetValue(item, out int shotNo);
            HOperatorSet.CopyImage(shots[shotNo], out HObject resultImage);
            return resultImage;
        }

        public AlignContext GetGridAlignContext(int packageIndex, List<HObject> images, HObject teachingImage, GridLgaTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(GridPackageTeachingService);

            AlignContext alignContext = new AlignContext();

            List<HObject> rotatedShots = images
                .Select(image => VisionOperation.RotateImage(image, teaching.RotateAngle))
                .ToList();

            List<Roi> packageRois = GridPackageTeachingService.PartitionRoi(teaching).ToList();

            teaching.ShotNumberForInspection.TryGetValue(LgaInspectionItem.PackageOffset, out int shotNo);

            var packageRoi = packageRois[packageIndex];

            HOperatorSet.CopyImage(images[shotNo], out HObject imageForPackage);

            var result = GridPackageTeachingService.InspectSinglePackage(
                image: imageForPackage,
                teaching: teaching,
                packageRoi: packageRoi,
                packageIndex: packageIndex,
                camera: camera,
                enforceAllChecks: true,
                inspectionItems: [],
                out _
                );

            alignContext.PackageRegion = result.PackageRegion;
            alignContext.PackagePoints = result.PackagePoints;

            MapEngine.AlignImageWithPose(
                teachingImage,
                alignContext.PackageRegion,
                teaching.PackageCenter,
                out HObject tmpAlignedImage,
                out alignContext.TransformMatrix,
                out alignContext.TransformMatrixInvert,
                out _
                );

            VisionOperation.AffineTransformRegion(alignContext.PackageRegion, alignContext.TransformMatrix, out HObject tmpAlignedPackageRegion);
            HOperatorSet.ErosionCircle(tmpAlignedPackageRegion, out HObject erodedPackageRegion, 10);
            VisionOperation.ReduceDomain(tmpAlignedImage, erodedPackageRegion, out HObject inspectionImage);

            alignContext.AlignedImage = inspectionImage;
            return alignContext;
        }

        public AlignContext GetSingleAlignContext(HObject teachingImage, GridLgaTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }

    }
}
