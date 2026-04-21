using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching.Ball;
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
using GVisionWpf.GlobalStates;
using GVisionWpf.Interfaces.Teaching.Ball;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public partial class GridBgaTeachingInspectionService : ITeachingInspectionService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, GridBgaTeaching teaching, ECamera camera, HashSet<BgaInspectionItem> inspectionItems)
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

                BgaInspectionResult result = new BgaInspectionResult
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
                        HObject imageForPackage = getInspectionImage(teaching, rotatedShots, BgaInspectionItem.PackageOffset);

                        using (imageForPackage)
                        {
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
                    }

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

                    if (!teaching.DontCareRois.IsNullOrEmpty())
                    {
                        for (int i = 0; i < inspectionShots.Count; i++)
                        {
                            inspectionShots[i] = inspectionShots[i].OmitRegionFromTarget(teaching.DontCareRois.ToList().Rois2Regions(), 2);
                        }
                    }

                    if (RejectMarkTeachingService != null)
                    {
                        using HObject imageForRejectMark = getInspectionImage(teaching, inspectionShots, BgaInspectionItem.RejectMark);
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
                        using HObject imageForFirstPin = getInspectionImage(teaching, inspectionShots, BgaInspectionItem.FirstPin);
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

                    if (PatternTeachingService != null)
                    {
                        using HObject imageForPattern = getInspectionImage(teaching, inspectionShots, BgaInspectionItem.Pattern);
                        alignContext.AlignedImage = imageForPattern;

                        PatternTeachingService.InspectPatterns(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData patternRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(patternRender);
                    }

                    if (BallTeachingService != null)
                    {
                        using HObject imageForBall = getInspectionImage(teaching, inspectionShots, BgaInspectionItem.BallCount);
                        alignContext.AlignedImage = imageForBall;

                        BallTeachingService.InspectBalls(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData ballRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(ballRender);
                    }

                    if (SurfaceTeachingService != null)
                    {
                        using HObject imageForSurface = getInspectionImage(teaching, inspectionShots, BgaInspectionItem.Scratch);
                        alignContext.AlignedImage = imageForSurface;
                        var surfaceService = new SurfaceTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);

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
                            tolerance: GlobalSetting.Instance.Inspection.Tolerance.MapCornerDegree,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out _
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        using HObject imageForSawOffset = getInspectionImage(teaching, inspectionShots, BgaInspectionItem.SawOffset);
                        alignContext.AlignedImage = imageForSawOffset;

                        SawingTeachingService.InspectSawOffset(
                            alignContext: alignContext,
                            xTolerance: GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetX,
                            yTolerance: GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetY,
                            camera: ECamera.Mapping,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out _
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        using HObject imageForChipping = getInspectionImage(teaching, rotatedShots, BgaInspectionItem.Chipping);
                        alignContext.AlignedImage = imageForSawOffset;

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

        public IEnumerable<BgaInspectionResult> Inspect(List<HObject> images, GridBgaTeaching teaching, ECamera camera, HashSet<BgaInspectionItem> inspectionItems, out List<InspectionRenderData> renderResults)
        {
            ArgumentNullException.ThrowIfNull(GridPackageTeachingService);

            ConcurrentBag<InspectionRenderData> renderBag = new ConcurrentBag<InspectionRenderData>();
            ConcurrentBag<BgaInspectionResult> resultBag = new ConcurrentBag<BgaInspectionResult>();

            renderResults = [];

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


            return [];
        }

        public IGridPackageTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? GridPackageTeachingService { get; set; }

        public IFirstPinTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? FirstPinTeachingService { get; set; }

        public IPatternTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? PatternTeachingService { get; set; }

        public IBallTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? BallTeachingService { get; set; }

        public ISurfaceTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? SurfaceTeachingService { get; set; }

        public IRejectMarkTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? RejectMarkTeachingService { get; set; }

        public ISawingTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? SawingTeachingService { get; set; }

        #region unused
       
        public IDataCodeTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? DataCodeTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMarkTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? MarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMultiPadTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? MultiPadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISinglePadTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? SinglePadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ILeadTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? LeadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISinglePackageTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>? SinglePackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        public GridBgaTeachingInspectionService()
        {
            GridPackageTeachingService = new GridPackageTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            FirstPinTeachingService = new FirstPinTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            PatternTeachingService = new PatternTeachingInspectionService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            BallTeachingService = new BallTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            SurfaceTeachingService = new SurfaceTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new BgaSawingTeachingService<GridBgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
        }
    }

    partial class GridBgaTeachingInspectionService
    {
        private HObject getInspectionImage(GridBgaTeaching teaching, List<HObject> shots, BgaInspectionItem item)
        {
            teaching.ShotNumberForInspection.TryGetValue(item, out int shotNo);
            HOperatorSet.CopyImage(shots[shotNo], out HObject resultImage);
            return resultImage;
        }

        public AlignContext GetGridAlignContext(int packageIndex, List<HObject> images, HObject teachingImage, GridBgaTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(GridPackageTeachingService);

            AlignContext alignContext = new AlignContext();

            List<HObject> rotatedShots = images
                .Select(image => VisionOperation.RotateImage(image, teaching.RotateAngle))
                .ToList();

            List<Roi> packageRois = GridPackageTeachingService.PartitionRoi(teaching).ToList();

            teaching.ShotNumberForInspection.TryGetValue(BgaInspectionItem.PackageOffset, out int shotNo);

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

        public AlignContext GetSingleAlignContext(HObject teachingImage, GridBgaTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }
    }
}
