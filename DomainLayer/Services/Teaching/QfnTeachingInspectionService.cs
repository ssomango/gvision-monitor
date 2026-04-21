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
    public partial class QfnTeachingInspectionService : ITeachingInspectionService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, QfnTeaching teaching, ECamera camera, HashSet<QfnInspectionItem> inspectionItems)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            List<RenderableInspectionResult> results = new List<RenderableInspectionResult>();

            HObject resultImage = images.First()
                .CopyImage()
                .DisposeBy(DisposeBag);

            Stopwatch stopwatch = Stopwatch.StartNew();

            QfnInspectionResult result = new QfnInspectionResult
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

                    if (result.HasDevice.Type != EResultType.Good)
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

                if (SinglePadTeachingService != null)
                {
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


        public ISinglePackageTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? SinglePackageTeachingService { get; set; }

        public IFirstPinTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? FirstPinTeachingService { get; set; }

        public ISinglePadTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? SinglePadTeachingService { get; set; }

        public ILeadTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? LeadTeachingService { get; set; }

        public ISurfaceTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? SurfaceTeachingService { get; set; }

        public IRejectMarkTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? RejectMarkTeachingService { get; set; }

        public ISawingTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? SawingTeachingService { get; set; }

        #region unused
        public IGridPackageTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? GridPackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMultiPadTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? MultiPadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDataCodeTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? DataCodeTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMarkTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? MarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }   
        public IBallTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? BallTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPatternTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>? PatternTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        public QfnTeachingInspectionService()
        {
            SinglePackageTeachingService = new SinglePackageTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            FirstPinTeachingService = new FirstPinTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            SinglePadTeachingService = new SinglePadTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            LeadTeachingService = new LeadTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);

            SurfaceTeachingService = new SurfaceTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new LeadAndPadSawingTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>().DisposeBy(DisposeBag);
        }
    }

    partial class QfnTeachingInspectionService
    {
        public AlignContext GetGridAlignContext(int packageNumber, List<HObject> images, HObject teachingImage, QfnTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }

        public AlignContext GetSingleAlignContext(HObject teachingImage, QfnTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            var alignContext = new AlignContext();

            SinglePackageTeachingService.InspectPackage(teachingImage, teaching, camera, true, [], out alignContext, out _);

            return alignContext;
        }
    }
}
