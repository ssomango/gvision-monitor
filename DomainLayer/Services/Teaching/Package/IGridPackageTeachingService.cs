using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.Package
{
    public partial interface IGridPackageTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public IGridPackageTeachingModel<TTeaching> TeachAutoThreshold(HObject teachingImage, IGridPackageTeachingModel<TTeaching> teaching);

        public IEnumerable<Roi> PartitionRoi(IGridPackageTeachingModel<TTeaching> teaching);

        public IPackageInspectionResultModel<TResult> InspectSinglePackage(HObject image, IGridPackageTeachingModel<TTeaching> teaching, Roi packageRoi, int packageIndex, ECamera camera, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);

        public IEnumerable<IPackageInspectionResultModel<TResult>> InspectGridPackages(HObject teachingImage, IGridPackageTeachingModel<TTeaching> teaching, ECamera camera, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData);
    }
}
