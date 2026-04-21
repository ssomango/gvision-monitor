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
using System.Diagnostics;
using System.Threading.Tasks;

namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public sealed partial class MoldTeachingInspectionService : ITeachingInspectionService<MoldTeaching, MapInspectionResult, MoldInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, MoldTeaching teaching, ECamera camera, HashSet<MoldInspectionItem> inspectionItems)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            List<RenderableInspectionResult> results = new List<RenderableInspectionResult>();

            HObject resultImage = images.First()
                .CopyImage()
                .DisposeBy(DisposeBag);

            Stopwatch stopwatch = Stopwatch.StartNew();

            MapInspectionResult result = new MapInspectionResult
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

                ArgumentNullException.ThrowIfNull(result.PackageRegion);

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
                    }

                }

                if (DataCodeTeachingService != null)
                {
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
                        out InspectionRenderData cornerDegreeRender
                        )
                        .MergeTo(result)
                        .DisposeBy(DisposeBag);

                    renderData.MergeWith(cornerDegreeRender);

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

                EResultType resultType = InspectionResultConverter.ErrorTypeInEResultType(result);
                FixedText totalText = new FixedText("Result : " + resultType.ToString().ToUpper(), 1, resultType == EResultType.Good ? EColor.Green : EColor.Red);
                renderData.AddText(totalText);

                results.Add(new RenderableInspectionResult(result, renderData));
                results = results.OrderBy(r => r.InspectionResult.PackageNoInFov).ToList();
            }

            return results;
        }

      
        public ISinglePackageTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? SinglePackageTeachingService { get; set; }

        public IDataCodeTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? DataCodeTeachingService { get; set; }

        public IMarkTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? MarkTeachingService { get; set; }

        public ISurfaceTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? SurfaceTeachingService { get; set; }

        public IRejectMarkTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? RejectMarkTeachingService { get; set; }

        public ISawingTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? SawingTeachingService { get; set; }


        #region UnUsed
        public IGridPackageTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? GridPackageTeachingService { get; set; }
        public ISinglePadTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? SinglePadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IFirstPinTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? FirstPinTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IBallTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? BallTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IMultiPadTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? MultiPadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ILeadTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? LeadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IPatternTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>? PatternTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion


        public MoldTeachingInspectionService()
        {
            SinglePackageTeachingService = new SinglePackageTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            MarkTeachingService = new MarkTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            DataCodeTeachingService = new DataCodeTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            SurfaceTeachingService = new MapSurfaceTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            SawingTeachingService = new MapSawingTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
            RejectMarkTeachingService = new RejectMarkTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>().DisposeBy(DisposeBag);
        }

        public void Dispose() => DisposeBag.Dispose();

        ~MoldTeachingInspectionService() => Dispose();
    }

    partial class MoldTeachingInspectionService
    {
        public AlignContext GetGridAlignContext(int packageNumber, List<HObject> images, HObject teachingImage, MoldTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }

        public AlignContext GetSingleAlignContext(HObject teachingImage, MoldTeaching teaching, ECamera camera)
        {
            ArgumentNullException.ThrowIfNull(SinglePackageTeachingService);

            var alignContext = new AlignContext();

            SinglePackageTeachingService.InspectPackage(teachingImage, teaching, camera, true, [], out alignContext, out _);

            return alignContext;
        }
    }
}
