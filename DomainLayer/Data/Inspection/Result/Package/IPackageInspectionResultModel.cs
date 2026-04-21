using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Package
{
    public interface IPackageInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<bool> HasDevice { get; set; }

        public Result<Pose> PackageOffset { get; set; }

        public Result<Size> PackageSize { get; set; }


        public HObject? PackageRegion { get; set; }

        public List<Point> PackagePoints { get; set; }

        public IPackageInspectionResultModel<T> MergeTo(IPackageInspectionResultModel<T> model)
        {
            model.HasDevice = HasDevice;
            model.PackageOffset = PackageOffset;
            model.PackageSize = PackageSize;
            model.PackageRegion = PackageRegion;
            model.PackagePoints = PackagePoints;
            return model;
        }
    }
}
