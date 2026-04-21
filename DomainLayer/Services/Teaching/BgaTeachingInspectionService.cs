using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
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
using GVisionWpf.DomainLayer.Services.Teaching.Strip;
using GVisionWpf.DomainLayer.Services.Teaching.Surface;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Interfaces.Teaching.Ball;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public partial class BgaTeachingInspectionService : ITeachingInspectionService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, BgaTeaching teaching, ECamera camera, HashSet<BgaInspectionItem> inspectionItems)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            List<RenderableInspectionResult> results = new List<RenderableInspectionResult>();

            Stopwatch stopwatch = Stopwatch.StartNew();
            
            AlignContext alignContext = new AlignContext();

            InspectionRenderData renderData = new InspectionRenderData();

            HObject resultImage = images.First()
                                        .CopyImage()
                                        .DisposeBy(DisposeBag);

            BgaInspectionResult result = new BgaInspectionResult
            {
                Image = resultImage,
                StartTime = DateTime.Now,
                Type = EInspection.Lga,
            }
            .DisposeBy(DisposeBag);

            try
            {
              
                if (SinglePackageTeachingService != null)
                {
                    SinglePackageTeachingService.InspectPackage(
                        image: resultImage,
                        teaching: teaching,
                        camera: camera,
                        enforceAllChecks: false,
                        inspectionItems: inspectionItems,
                        out alignContext,
                        out InspectionRenderData packageRender
                        )
                        .MergeTo(result)
                        .DisposeBy(DisposeBag);

                    renderData.MergeWith(packageRender);

                    if (result.HasDevice.Type == EResultType.NoDevice)
                    {
                        return results;
                    }
                }

                if (RejectMarkTeachingService != null)
                {
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
                        return results;
                    }
                }

                if (FirstPinTeachingService != null)
                {
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
                    var surfaceTeachingService = new SurfaceTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>()
                        .DisposeBy(DisposeBag);

                    surfaceTeachingService.InspectScratches(
                        alignContext: alignContext,
                        teaching: teaching,
                        enforceAllChecks: false,
                        inspectionItems: inspectionItems,
                        out InspectionRenderData surfaceRender
                        )
                        .MergeTo(result)
                        .DisposeBy(DisposeBag);

                    renderData.MergeWith(surfaceRender);

                    surfaceTeachingService.InspectForeignMaterials(
                       alignContext: alignContext,
                       teaching: teaching,
                       enforceAllChecks: false,
                       inspectionItems: inspectionItems,
                       out InspectionRenderData foreignMaterialRender
                       )
                       .MergeTo(result)
                       .DisposeBy(DisposeBag);
                    
                    renderData.MergeWith(foreignMaterialRender);

                    surfaceTeachingService.InspectContaminations(
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

                    SawingTeachingService.InspectSawOffset(
                        alignContext: alignContext,
                        xTolerance: GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX,
                        yTolerance: GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetY,
                        camera: ECamera.Mapping,
                        teaching: teaching,
                        enforceAllChecks: false,
                        inspectionItems: inspectionItems,
                        out _
                        )
                        .MergeTo(result)
                        .DisposeBy(DisposeBag);


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

                
                EResultType resultType = InspectionResultConverter.ErrorTypeInEResultType(result);
                FixedText totalText = new FixedText("Result : " + resultType.ToString().ToUpper(), 1, resultType == EResultType.Good ? EColor.Green : EColor.Red);

                renderData.AddText(totalText);
                
                results.Add(new RenderableInspectionResult(result, renderData));
                results = results.OrderBy(r => r.InspectionResult.PackageNoInFov).ToList();

                foreach (var r in results)
                {
                    var tr = new List<(HObject, EColor)>();

                    foreach (var d in r.RenderData.ResultDrawings)
                    {
                        var transformed = d.drawingObject.AffineTransformRegion(alignContext.TransformMatrixInvert);
                        tr.Add((transformed, d.color));

                        d.drawingObject.Dispose();
                    }

                    r.RenderData.ResultDrawings = tr;
                }
            }

            return results;
        }

        public ISinglePackageTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? SinglePackageTeachingService { get; set; }

        public IFirstPinTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? FirstPinTeachingService { get; set; }

        public IPatternTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? PatternTeachingService { get; set; }

        public IBallTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? BallTeachingService { get; set; }

        public ISurfaceTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? SurfaceTeachingService { get; set; }

        public IRejectMarkTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? RejectMarkTeachingService { get; set; }

        public ISawingTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? SawingTeachingService { get; set; }

        #region unused
        public IGridPackageTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? GridPackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDataCodeTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? DataCodeTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMarkTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? MarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMultiPadTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? MultiPadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISinglePadTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? SinglePadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ILeadTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? LeadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IStripTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>? StripTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        public BgaTeachingInspectionService()
        {
            SinglePackageTeachingService = new SinglePackageTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            FirstPinTeachingService = new FirstPinTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            PatternTeachingService = new PatternTeachingInspectionService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            BallTeachingService = new BallTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            SurfaceTeachingService = new SurfaceTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new BgaSawingTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>().DisposeBy(DisposeBag);

            GVisionMessenger.Instance.RegisterAll(this);
        }
    }

    partial class BgaTeachingInspectionService
    {
        public AlignContext GetGridAlignContext(int packageNumber, List<HObject> images, HObject teachingImage, BgaTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }

        public AlignContext GetSingleAlignContext(HObject teachingImage, BgaTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            var alignContext = new AlignContext();

            SinglePackageTeachingService.InspectPackage(teachingImage, teaching, camera, true, [], out alignContext, out _);

            return alignContext;
        }
    }

    partial class BgaTeachingInspectionService : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.ClearAllResults:
                    DisposeBag.Clear();
                    break;

                default:
                    return;
            }
        }
    }
}
