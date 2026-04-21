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
using GVisionWpf.GlobalStates;
using GVisionWpf.Interfaces.Teaching.Ball;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public partial class GridQfnTeachingInspectionService : ITeachingInspectionService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, GridQfnTeaching teaching, ECamera camera, HashSet<QfnInspectionItem> inspectionItems)
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

                QfnInspectionResult result = new QfnInspectionResult
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
                        HObject imageForPackage = getInspectionImage(teaching, rotatedShots, QfnInspectionItem.PackageOffset);

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
                        using HObject imageForRejectMark = getInspectionImage(teaching, inspectionShots, QfnInspectionItem.RejectMark);
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
                        using HObject imageForFirstPin = getInspectionImage(teaching, inspectionShots, QfnInspectionItem.FirstPin);
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

                    if (SinglePadTeachingService != null)
                    {
                        using HObject imageForSinglePad = getInspectionImage(teaching, inspectionShots, QfnInspectionItem.PadSize);
                        alignContext.AlignedImage = imageForSinglePad;

                        SinglePadTeachingService.InspectPad(
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
                        using HObject imageForLeads = getInspectionImage(teaching, inspectionShots, QfnInspectionItem.LeadSize);
                        alignContext.AlignedImage = imageForLeads;

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
                        using HObject imageForSurface = getInspectionImage(teaching, inspectionShots, QfnInspectionItem.Scratch);
                        alignContext.AlignedImage = imageForSurface;
                        var surfaceService = new SurfaceTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);

                        surfaceService.InspectScratches(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData scratchRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(scratchRender);

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

                        using HObject imageForSawOffset = getInspectionImage(teaching, inspectionShots, QfnInspectionItem.SawOffset);
                        alignContext.AlignedImage = imageForSawOffset;

                        SawingTeachingService.InspectSawOffset(
                            alignContext: alignContext,
                            xTolerance: GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetX,
                            yTolerance: GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetY,
                            camera: ECamera.Mapping,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out _
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        using HObject imageForChipping = getInspectionImage(teaching, rotatedShots, QfnInspectionItem.Chipping);
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
                            out InspectionRenderData burrrRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(burrrRender);
                    }
                }
                finally
                {
                    result.Duration = stopwatch.ElapsedMilliseconds;
                    stopwatch.Stop();

                    results.Add(new RenderableInspectionResult(result, renderData));
                }
            });

            var orederedResults = results.OrderBy(r => r.InspectionResult.PackageNoInFov).ToList();

            int nGoodDevices = orederedResults.Count(result => result.InspectionResult.EvaluateResults());
            int nBadDevices = orederedResults.Count(result => !result.InspectionResult.EvaluateResults());

            FixedText totalText = new FixedText($"TOTAL: {nGoodDevices + nBadDevices}, GOOD: {nGoodDevices}, BAD: {nBadDevices}", 1, nBadDevices > 0 ? EColor.Red : EColor.Green);
            orederedResults.ForEach(r => r.RenderData.AddText(totalText));

            return results;
        }

      
        public IGridPackageTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? GridPackageTeachingService { get; set; }

        public IFirstPinTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? FirstPinTeachingService { get; set; }

        public ISinglePadTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? SinglePadTeachingService { get; set; }

        public ILeadTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? LeadTeachingService { get; set; }

        public ISurfaceTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? SurfaceTeachingService { get; set; }

        public IRejectMarkTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? RejectMarkTeachingService { get; set; }

        public ISawingTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? SawingTeachingService { get; set; }

        #region unused
        public ISinglePackageTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? SinglePackageTeachingService { get; set; }
        public IMultiPadTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? MultiPadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDataCodeTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? DataCodeTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMarkTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? MarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IBallTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? BallTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPatternTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>? PatternTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion


        public GridQfnTeachingInspectionService()
        {
            GridPackageTeachingService = new GridPackageTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            FirstPinTeachingService = new FirstPinTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            SinglePadTeachingService = new SinglePadTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            LeadTeachingService = new LeadTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);

            SurfaceTeachingService = new SurfaceTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new LeadAndPadSawingTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<GridQfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
        }
    }

    public partial class GridQfnTeachingInspectionService
    {
        private HObject getInspectionImage(GridQfnTeaching teaching, List<HObject> shots, QfnInspectionItem item)
        {
            teaching.ShotNumberForInspection.TryGetValue(item, out int shotNo);
            HOperatorSet.CopyImage(shots[shotNo], out HObject resultImage);
            return resultImage;
        }

        public AlignContext GetGridAlignContext(int packageIndex, List<HObject> images, HObject teachingImage, GridQfnTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(GridPackageTeachingService);

            AlignContext alignContext = new AlignContext();

            List<HObject> rotatedShots = images
                .Select(image => VisionOperation.RotateImage(image, teaching.RotateAngle))
                .ToList();

            List<Roi> packageRois = GridPackageTeachingService.PartitionRoi(teaching).ToList();

            teaching.ShotNumberForInspection.TryGetValue(QfnInspectionItem.PackageOffset, out int shotNo);

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

        public AlignContext GetSingleAlignContext(HObject teachingImage, GridQfnTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }
    }
}
