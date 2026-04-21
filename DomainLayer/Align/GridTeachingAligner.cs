using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Align
{
    public sealed class GridTeachingAligner<T> : ITeachingImageAlignable<T> where T : InspectionTeaching
    {
        public DisposeBag DisposeBag { get; set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public void AlignTeachingImage(HObject teachingImage, HObject packageRegion, IBasePackageModel<T> teaching, out HObject alignedImage, out HTuple homMat2D, out HTuple invHomMat2D)
        {
            MapEngine.AlignImageWithPose(teachingImage, packageRegion, teaching.PackageCenter, out HObject tmpAlignedImage, out homMat2D, out invHomMat2D, out HTuple composedHomMat2D);
            DisposeBag.Add(tmpAlignedImage, homMat2D, invHomMat2D, composedHomMat2D);

            HObject tmpAlignedPackageRegion = packageRegion
                .AffineTransformRegion(homMat2D)
                .DisposeBy(DisposeBag);

            HOperatorSet.ErosionCircle(tmpAlignedPackageRegion, out HObject erodedPackageRegion, 10);

            VisionOperation.ReduceDomain(tmpAlignedImage, erodedPackageRegion, out alignedImage);
            alignedImage.DisposeBy(DisposeBag);

            if (teaching is IDontCareTeachingModel<T> dontCareTeaching &&
                dontCareTeaching.DontCareRois.Count > 0)
            {
                VisionEngine.OmitRegionFromTarget(alignedImage, dontCareTeaching.DontCareRois.ToList(), 2, out alignedImage);
            }
        }
    }
}
