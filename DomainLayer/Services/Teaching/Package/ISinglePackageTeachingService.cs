using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.Package
{
    public partial interface ISinglePackageTeachingService<TTeaching, TResult, TItem> : IDisposable
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public EInspection Inspection
        {
            get
            {
                if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))) return EInspection.Lga;
                else if (typeof(BgaTeaching).IsAssignableFrom(typeof(TTeaching))) return EInspection.Bga;
                else if (typeof(QfnTeaching).IsAssignableFrom(typeof(TTeaching))) return EInspection.Qfn;
                else throw new NotImplementedException($"Teaching type {typeof(TTeaching).Name} is not implemented.");
            }
        }

        public ISinglePackageTeachingModel<TTeaching> TrainPackage(HObject teachingImage, ISinglePackageTeachingModel<TTeaching> teaching);

        public ISinglePackageTeachingModel<TTeaching> TeachAutoThreshold(HObject teachingImage, ISinglePackageTeachingModel<TTeaching> teaching, out InspectionRenderData renderData);

        public ISinglePackageTeachingModel<TTeaching> TeachAutoRoi(HObject teachingImage, ISinglePackageTeachingModel<TTeaching> teaching, out InspectionRenderData renderData);

        public IPackageInspectionResultModel<TResult> InspectPackage(HObject image, ISinglePackageTeachingModel<TTeaching> teaching, ECamera camera, bool enforceAllChecks, HashSet<TItem> inspectionItems, out AlignContext alignContext, out InspectionRenderData renderData);
    }
}
