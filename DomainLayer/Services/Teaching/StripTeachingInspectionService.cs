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
using GVisionWpf.DomainLayer.Services.Teaching.Strip;
using GVisionWpf.DomainLayer.Services.Teaching.Surface;
using GVisionWpf.Interfaces.Teaching.Ball;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public class StripTeachingInspectionService : ITeachingInspectionService<StripTeaching, StripInspectionResult, IInspectionItem>
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public async Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, StripTeaching teaching, ECamera camera, HashSet<IInspectionItem> inspectionItems)
        {
            List<RenderableInspectionResult> results = new List<RenderableInspectionResult>();

            var image = images.First();

            Stopwatch stopwatch = Stopwatch.StartNew();

            StripInspectionResult result = new StripInspectionResult
            {
                    Image = image,
                    XPosition = 0,
                    YPosition = 0,
                    StartTime = DateTime.Now,
                    Type = EInspection.DataCode,
            };

            InspectionRenderData renderData = new InspectionRenderData();

            try
            {
                if (StripTeachingService is not null)
                {

                    StripTeachingService.InspectStripDataCode(
                        image: image,
                        teaching: teaching,
                        out InspectionRenderData barcodeRedner
                        )
                        .MergeTo(result);

                    renderData.MergeWith(barcodeRedner);
                }
            }
            finally
            {
                result.Duration = stopwatch.ElapsedMilliseconds;
                stopwatch.Stop();

                results.Add(new RenderableInspectionResult(result, renderData));
                results = results.OrderBy(r => r.InspectionResult.PackageNoInFov).ToList();
            }

            return results;
        }


        public IStripTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? StripTeachingService { get; set; }

        #region unused
        public ISinglePackageTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? SinglePackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGridPackageTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? GridPackageTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFirstPinTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? FirstPinTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPatternTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? PatternTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDataCodeTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? DataCodeTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMarkTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? MarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMultiPadTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? MultiPadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISinglePadTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? SinglePadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ILeadTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? LeadTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IBallTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? BallTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISurfaceTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? SurfaceTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IRejectMarkTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? RejectMarkTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISawingTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>? SawingTeachingService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        public StripTeachingInspectionService()
        {
            StripTeachingService = new StripTeachingService<StripTeaching, StripInspectionResult, IInspectionItem>().DisposeBy(DisposeBag);
        }


        public AlignContext GetGridAlignContext(int packageNumber, List<HObject> images, HObject teachingImage, StripTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }

        public AlignContext GetSingleAlignContext(HObject teachingImage, StripTeaching teaching, ECamera camera)
        {
            throw new NotImplementedException();
        }
    }
}
