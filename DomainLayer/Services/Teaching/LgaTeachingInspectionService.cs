using System.Diagnostics;
using System.Threading.Tasks;
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

namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public sealed partial class LgaTeachingInspectionService : ITeachingInspectionService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();


        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, LgaTeaching teaching, ECamera camera, HashSet<LgaInspectionItem> inspectionItems)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            List<RenderableInspectionResult> results = new List<RenderableInspectionResult>();

            HObject resultImage = images.First()
                .CopyImage()
                .DisposeBy(DisposeBag);

            Stopwatch stopwatch = Stopwatch.StartNew();

            LgaInspectionResult result = new LgaInspectionResult
            {
                Image = resultImage,
                StartTime = DateTime.Now,
                Type = EInspection.Lga,
            }
            .DisposeBy(DisposeBag);

            AlignContext alignContext = new AlignContext();
            InspectionRenderData renderData = new InspectionRenderData();

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

                if (MultiPadTeachingService != null)
                {
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
                    SurfaceTeachingService.InspectScratches(
                        alignContext: alignContext,
                        teaching: teaching,
                        enforceAllChecks: false,
                        inspectionItems: inspectionItems,
                        out InspectionRenderData scratchRender
                        )
                        .MergeTo(result)
                        .DisposeBy(DisposeBag);

                    renderData.MergeWith(scratchRender);

                    SurfaceTeachingService.InspectForeignMaterials(
                       alignContext: alignContext,
                       teaching: teaching,
                       enforceAllChecks: false,
                       inspectionItems: inspectionItems,
                       out InspectionRenderData foreignMaterialRender
                       )
                       .MergeTo(result)
                       .DisposeBy(DisposeBag);

                    renderData.MergeWith(foreignMaterialRender);


                    SurfaceTeachingService.InspectContaminations(
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
            }

            return results;
        }

        public ISinglePackageTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? SinglePackageTeachingService { get; set; }

        public IFirstPinTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? FirstPinTeachingService { get; set; }

        public IMultiPadTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? MultiPadTeachingService { get; set; }

        public ILeadTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? LeadTeachingService { get; set; }

        public ISurfaceTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? SurfaceTeachingService { get; set; }

        public ISawingTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? SawingTeachingService { get; set; }

        public IRejectMarkTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? RejectMarkTeachingService { get; set; }

        #region unused
        public IGridPackageTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? GridPackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDataCodeTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? DataCodeTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMarkTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? MarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IBallTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? BallTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISinglePadTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? SinglePadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPatternTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>? PatternTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        public LgaTeachingInspectionService()
        {
            SinglePackageTeachingService = new SinglePackageTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            FirstPinTeachingService = new FirstPinTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            MultiPadTeachingService = new MultiPadTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            LeadTeachingService = new LeadTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            SurfaceTeachingService = new SurfaceTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new LeadAndPadSawingTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>().DisposeBy(DisposeBag);
        }

        ~LgaTeachingInspectionService() => Dispose();
    }

    partial class LgaTeachingInspectionService
    {
        public AlignContext GetGridAlignContext(int packageNumber, List<HObject> images, HObject teachingImage, LgaTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }

        public AlignContext GetSingleAlignContext(HObject teachingImage, LgaTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            var alignContext = new AlignContext();

            SinglePackageTeachingService.InspectPackage(teachingImage, teaching, camera, true, [], out alignContext, out _);

            return alignContext;
        }
    }
}
