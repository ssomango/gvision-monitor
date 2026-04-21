using System.Threading.Tasks;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Package;
using GVisionWpf.DomainLayer.Data.TeachingModel;
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
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;


namespace GVisionWpf.DomainLayer.Services.Teaching
{
    public partial interface ITeachingInspectionService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public ISinglePackageTeachingService<TTeaching, TResult, TItem>? SinglePackageTeachingService { get; set; }

        public IGridPackageTeachingService<TTeaching, TResult, TItem>? GridPackageTeachingService { get; set; }

        public IFirstPinTeachingService<TTeaching, TResult, TItem>? FirstPinTeachingService { get; set; }

        public IPatternTeachingService<TTeaching, TResult, TItem>? PatternTeachingService { get; set; }

        public IDataCodeTeachingService<TTeaching, TResult, TItem>? DataCodeTeachingService { get; set; }

        public IMarkTeachingService<TTeaching, TResult, TItem>? MarkTeachingService { get; set; }

        public IMultiPadTeachingService<TTeaching, TResult, TItem>? MultiPadTeachingService { get; set; }

        public ISinglePadTeachingService<TTeaching, TResult, TItem>? SinglePadTeachingService { get; set; }

        public ILeadTeachingService<TTeaching, TResult, TItem>? LeadTeachingService { get; set; }

        public IBallTeachingService<TTeaching, TResult, TItem>? BallTeachingService { get; set; }

        public ISurfaceTeachingService<TTeaching, TResult, TItem>? SurfaceTeachingService { get; set; }

        public IRejectMarkTeachingService<TTeaching, TResult, TItem>? RejectMarkTeachingService { get; set; }

        public ISawingTeachingService<TTeaching, TResult, TItem>? SawingTeachingService { get; set; }

        public Task<IEnumerable<RenderableInspectionResult>> InspectAsync(List<HObject> images, TTeaching teaching, ECamera camera, HashSet<TItem> inspectionItems);

        public void GetPackageRegion(HObject image, TTeaching teaching, out HObject packageRegion, out List<Point> packagePoints)
        {
            if (teaching is ISinglePackageTeachingModel<TTeaching> singlePackageTeachingModel &&
                SinglePackageTeachingService != null)
            {
                VisionEngine.GetPackageRegion(
                    image: image,
                    top: singlePackageTeachingModel.PackageRoiTop,
                    bottom: singlePackageTeachingModel.PackageRoiBottom,
                    left: singlePackageTeachingModel.PackageRoiLeft,
                    right: singlePackageTeachingModel.PackageRoiRight,
                    direction: singlePackageTeachingModel.PackageEdgeDetectDirection,
                    detectMode: singlePackageTeachingModel.PackageEdgeDetectMode,
                    thresholdDiff: singlePackageTeachingModel.PackageThresholdDiff,
                   out packageRegion,
                   out packagePoints
               );
            }
            else if (teaching is IGridPackageTeachingModel<TTeaching> gridPackageTeachingModel &&
                GridPackageTeachingService != null)
            {
                VisionOperation.Roi2BorderBoxes(gridPackageTeachingModel.PackageRoi, out Roi top, out Roi bottom, out Roi left, out Roi right);

                VisionEngine.GetPackageRegion(
                    image: image,
                    top: top,
                    bottom: bottom,
                    left: left,
                    right: right,
                    direction: gridPackageTeachingModel.PackageEdgeDetectDirection,
                    detectMode: gridPackageTeachingModel.PackageEdgeDetectMode,
                    thresholdDiff: gridPackageTeachingModel.PackageThresholdDiff,
                    out packageRegion,
                    out packagePoints
                    );
            }
            else
            {
                throw new Exception("Invalid package teaching model type.");
            }
        }

        public AlignContext GetGridAlignContext(int packageNumber, List<HObject> images, HObject teachingImage, TTeaching teaching, ECamera camera);

        public AlignContext GetSingleAlignContext(HObject teachingImage, TTeaching teaching, ECamera camera);
    }
}
