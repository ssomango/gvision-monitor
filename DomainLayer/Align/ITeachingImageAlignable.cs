using GVisionWpf.DomainLayer.Data.TeachingModel;
namespace GVisionWpf.DomainLayer.Align
{
    public interface ITeachingImageAlignable<T> : IDisposable where T : InspectionTeaching
    {
        public DisposeBag DisposeBag { get; set; }

        public void AlignTeachingImage(HObject teachingImage, HObject packageRegion, IBasePackageModel<T> teaching, out HObject alignedImage, out HTuple homMat2D, out HTuple invHomMat2D);
    }
}
