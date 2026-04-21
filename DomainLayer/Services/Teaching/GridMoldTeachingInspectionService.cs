using GVisionWpf.Interfaces.Teaching.Ball;
using GVisionWpf.Models.Entities.Result;
using System.Diagnostics;
using GVisionWpf.Visions;
using GVisionWpf.GlobalStates;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.DomainLayer.Services.Teaching.DataCode;
using GVisionWpf.DomainLayer.Services.Teaching.FirstPin;
using GVisionWpf.DomainLayer.Services.Teaching.Lead;
using GVisionWpf.DomainLayer.Services.Teaching.Mark;
using GVisionWpf.DomainLayer.Services.Teaching.MultiPad;
using GVisionWpf.DomainLayer.Services.Teaching.Package;
using GVisionWpf.DomainLayer.Services.Teaching.RejectMark;
using GVisionWpf.DomainLayer.Services.Teaching.Sawing;
using GVisionWpf.DomainLayer.Services.Teaching.Surface;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching.Pad;
using GVisionWpf.DomainLayer.Services.Teaching.Pattern;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.Visions.Engines;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.Repositories;
using GVisionWpf.Events.Message.Packet;
using GVisionWpf.Events.Message.Inspection;

namespace GVisionWpf.Interfaces
{
    public sealed partial class GridMoldTeachingInspectionService : ITeachingInspectionService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>, IDisposable
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        private bool shouldTeachNewMarks = false;

        private void updateTeachingIfRequested(List<HObject> images, GridMoldTeaching teaching, ECamera camera, HashSet<MoldInspectionItem> inspectionItems, List<Roi> packageRois)
        {
            ArgumentNullException.ThrowIfNull(GridPackageTeachingService);

            if (shouldTeachNewMarks)
            {
                ArgumentNullException.ThrowIfNull(MarkTeachingService);

                using HObject imageForMark = getInspectionImage(teaching, images, MoldInspectionItem.WrongMark);

                Roi selectedPackageRoi = packageRois.ElementAt(teaching.SelectedPackageIndex - 1);

                var result = GridPackageTeachingService.InspectSinglePackage(
                    image: imageForMark,
                    teaching: teaching,
                    packageRoi: selectedPackageRoi,
                    packageIndex: teaching.SelectedPackageIndex - 1,
                    camera: camera,
                    enforceAllChecks: false,
                    inspectionItems: inspectionItems,
                    out _
                    );

                MarkTeachingService.TeachMarks(
                    imageForMark,
                     result.PackageRegion,
                     teaching,
                     null,
                     out _
                    )
                .MergeTo(teaching);

                shouldTeachNewMarks = false;

                GridMoldRepository.Instance.SaveRecipe(teaching);
            }
        }

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, GridMoldTeaching teaching, ECamera camera, HashSet<MoldInspectionItem> inspectionItems)
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
                HObject newResult;
                HOperatorSet.AddImage(resultImage, rotatedShots[i], out newResult, 0.5, 0);
                resultImage.Dispose();  // 기존 결과 해제
                resultImage = newResult.DisposeBy(DisposeBag);  // 새 결과 등록
            }

            List<Roi> packageRois = GridPackageTeachingService.PartitionRoi(teaching).ToList();

            updateTeachingIfRequested(images, teaching, camera, inspectionItems, packageRois);

            await Parallel.ForEachAsync(packageRois.Select((packageRoi, index) => (packageRoi, index)), async (pair, cancellationToken) =>
            {
                InspectionRenderData renderData = new InspectionRenderData();

                AlignContext alignContext = new AlignContext();

                Stopwatch stopwatch = Stopwatch.StartNew();

                MapInspectionResult result = new MapInspectionResult
                {
                    PackageNoInFov = pair.index + 1,
                    Shots = images,
                    Image = resultImage,
                    StartTime = DateTime.Now,
                    Type = EInspection.Mapping,
                }
                .DisposeBy(DisposeBag);

                try
                {
                    if (GridPackageTeachingService != null)
                    {
                        using HObject imageForPackage = getInspectionImage(teaching, rotatedShots, MoldInspectionItem.PackageOffset);

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

                        tmpAlignedPackageRegion.Dispose();
                        erodedPackageRegion.Dispose();
                        tmpAlignedImage.Dispose();
                        //inspectionImage.DisposeBy(DisposeBag);

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
                        using HObject imageForRejectMark = getInspectionImage(teaching, inspectionShots, MoldInspectionItem.RejectMark);
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

                    if (DataCodeTeachingService != null)
                    {
                        using HObject imageForDataCode = getInspectionImage(teaching, inspectionShots, MoldInspectionItem.WrongMark);
                        alignContext.AlignedImage = imageForDataCode;

                        DataCodeTeachingService.InspectDataCodes(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData dataCodeRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(dataCodeRender);
                    }

                    if (MarkTeachingService != null)
                    {
                        using HObject imageForMark = getInspectionImage(teaching, inspectionShots, MoldInspectionItem.WrongMark);
                        alignContext.AlignedImage = imageForMark;

                        MarkTeachingService.InspectMarks(
                            alignContext: alignContext,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData markRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(markRender);
                    }

                    if (SurfaceTeachingService != null)
                    {
                        using HObject imageForSurface = getInspectionImage(teaching, inspectionShots, MoldInspectionItem.Scratch);
                        alignContext.AlignedImage = imageForSurface;
                        var surfaceService = new MapSurfaceTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);

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
                            out InspectionRenderData cornerDegreeRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(cornerDegreeRender);

                        using HObject imageForSawOffset = getInspectionImage(teaching, inspectionShots, MoldInspectionItem.WrongMark);
                        alignContext.AlignedImage = imageForSawOffset;

                        SawingTeachingService.InspectSawOffset(
                            alignContext: alignContext,
                            xTolerance: GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX,
                            yTolerance: GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetY,
                            camera: ECamera.Mapping,
                            teaching: teaching,
                            enforceAllChecks: false,
                            inspectionItems: inspectionItems,
                            out InspectionRenderData sawOffsetRender
                            )
                            .MergeTo(result)
                            .DisposeBy(DisposeBag);

                        renderData.MergeWith(sawOffsetRender);


                        using HObject imageForChipping = getInspectionImage(teaching, rotatedShots, MoldInspectionItem.Chipping);
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
            
            foreach (var shot in rotatedShots) shot.Dispose();

            return orderedResults;
        }

        public IGridPackageTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? GridPackageTeachingService { get; set; }

        public IDataCodeTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? DataCodeTeachingService { get; set; }

        public IMarkTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? MarkTeachingService { get; set; }

        public ISurfaceTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? SurfaceTeachingService { get; set; }

        public IRejectMarkTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? RejectMarkTeachingService { get; set; }

        public ISawingTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? SawingTeachingService { get; set; }

        #region unused
        public ISinglePackageTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? SinglePackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IFirstPinTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? FirstPinTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IBallTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? BallTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IMultiPadTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? MultiPadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ILeadTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? LeadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ISinglePadTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? SinglePadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IPatternTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>? PatternTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        public GridMoldTeachingInspectionService()
        {
            GridPackageTeachingService = new GridPackageTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            MarkTeachingService = new MarkTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            DataCodeTeachingService = new DataCodeTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            SurfaceTeachingService = new MapSurfaceTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new MapSawingTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<GridMoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);

            GVisionMessenger.Instance.RegisterAll(this);
        }

        ~GridMoldTeachingInspectionService() => Dispose();
    }
    
    partial class GridMoldTeachingInspectionService
    {
        private HObject getInspectionImage(GridMoldTeaching teaching, List<HObject> shots, MoldInspectionItem item)
        {
            teaching.ShotNumberForInspection.TryGetValue(item, out int shotNo);
            HOperatorSet.CopyImage(shots[shotNo], out HObject resultImage);
            return resultImage;
        }

        public AlignContext GetGridAlignContext(int packageIndex, List<HObject> images, HObject teachingImage, GridMoldTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(GridPackageTeachingService);

            AlignContext alignContext = new AlignContext();

            /*
            List<HObject> rotatedShots = images
                .Select(image => VisionOperation.RotateImage(image, teaching.RotateAngle))
                .ToList();
            */

            List<Roi> packageRois = GridPackageTeachingService.PartitionRoi(teaching).ToList();
            teaching.ShotNumberForInspection.TryGetValue(MoldInspectionItem.PackageOffset, out int shotNo);

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

            imageForPackage.DisposeBy(DisposeBag);

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

            tmpAlignedPackageRegion.Dispose();
            erodedPackageRegion.Dispose();
            tmpAlignedImage.Dispose();
            //inspectionImage.Dispose();

            return alignContext;
        }

        public AlignContext GetSingleAlignContext(HObject teachingImage, GridMoldTeaching teaching, ECamera camera) => throw new NotImplementedException();
    }

    partial class GridMoldTeachingInspectionService : IRecipient<PacketMessage>
    {
        public void Receive(PacketMessage message)
        {
            switch (message.Action)
            {
                case PacketMessage.EPacketMessageAction.ShouldTeachNewMarks:
                    if (GlobalSetting.Instance.SystemType == ESystemType.HanaMicron)
                    {
                        shouldTeachNewMarks = true;
                    }
                    break;

                default:
                    break;
            }
        }
    }

    partial class GridMoldTeachingInspectionService : IRecipient<MoldInspectionUIUpdateMessage>
    {
        public void Receive(MoldInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EMoldInspectionUIUpdateType.ClearAllResults:
                    DisposeBag.Clear();
                    break;

                default:
                    return;
            }
        }
    }
}
