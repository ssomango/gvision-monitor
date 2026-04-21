using System.Windows.Navigation;
using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.Package
{
    public sealed class SinglePackageTeachingService<TTeaching, TResult, TItem> : ISinglePackageTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IPackageItemProvider<TItem>? packageItemProvider = PackageItemProviderFactory.GetProvider<TItem>();

        private Size OriginPackageSize => DeviceRecipeRepository.Instance.GetRecipe().PackageSize;

        private Size PackageSizeTolerance
        {
            get
            {
                if (typeof(BgaTeaching).IsAssignableFrom(typeof(TTeaching))) return GlobalSetting.Instance.Inspection.Tolerance.BgaPackageSize;
                else if (typeof(LgaTeaching).IsAssignableFrom(typeof(TTeaching))) return GlobalSetting.Instance.Inspection.Tolerance.LgaPackageSize;
                else if (typeof(QfnTeaching).IsAssignableFrom(typeof(TTeaching))) return GlobalSetting.Instance.Inspection.Tolerance.QfnPackageSize;
                else if (typeof(MoldTeaching).IsAssignableFrom(typeof(TTeaching))) return GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize;
                else throw new NotImplementedException($"Teaching type {typeof(TTeaching).Name} is not implemented.");
            }
        }

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public ISinglePackageTeachingModel<TTeaching> TrainPackage(HObject teachingImage, ISinglePackageTeachingModel<TTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(teaching.PackageModelRoi);

            var inspectedTeaching = DeepCopy.Copy(teaching);

            VisionEngine.GetPackageRegion(
                image: teachingImage,
                top: inspectedTeaching.PackageRoiTop,
                bottom: inspectedTeaching.PackageRoiBottom,
                left: inspectedTeaching.PackageRoiLeft,
                right: inspectedTeaching.PackageRoiRight,
                direction: inspectedTeaching.PackageEdgeDetectDirection,
                detectMode: inspectedTeaching.PackageEdgeDetectMode,
                thresholdDiff: inspectedTeaching.PackageThresholdDiff,
                out HObject packageRegion,
                out List<Point> packagePoints
                );

            VisionOperation.GetRegionOrientationOfSmallestRectangle2(packageRegion, out Pose pxPose, out _);
            inspectedTeaching.PackageCenter = pxPose;

            VisionOperation.CropImage(teachingImage, inspectedTeaching.PackageModelRoi, out HObject croppedImage);
            HOperatorSet.GetImageSize(croppedImage, out HTuple width, out HTuple height);

            HOperatorSet.BinaryThreshold(croppedImage, out HObject region, "max_separability", "light", out _);
            croppedImage.Dispose();
            HOperatorSet.RegionToBin(region, out HObject binaryImage, 255, 0, width, height);
            region.Dispose();

            HOperatorSet.CreateGenericShapeModel(out HTuple modelHandle);
            HOperatorSet.SetGenericShapeModelParam(modelHandle, "num_matches", 1);
            HOperatorSet.SetGenericShapeModelParam(modelHandle, "num_levels", 3);
            HOperatorSet.SetGenericShapeModelParam(modelHandle, "max_deformation", 2);
            HOperatorSet.SetGenericShapeModelParam(modelHandle, "subpixel", "least_squares_very_high");
            HOperatorSet.SetGenericShapeModelParam(modelHandle, "angle_start", new HTuple(-90).TupleRad());
            HOperatorSet.SetGenericShapeModelParam(modelHandle, "angle_end", new HTuple(90).TupleRad());
            HOperatorSet.TrainGenericShapeModel(binaryImage, modelHandle);
            binaryImage.Dispose();

            HOperatorSet.FindGenericShapeModel(teachingImage, modelHandle, out HTuple matchResultId, out _);
            HOperatorSet.GetGenericShapeModelResult(matchResultId, "best", "hom_mat_2d", out HTuple HomMat2DModelForAlign);

            inspectedTeaching.HomMat2DModelForAlign = HomMat2DModelForAlign;
            inspectedTeaching.ModelHandleForAlign = modelHandle;

            return inspectedTeaching;
        }

        public ISinglePackageTeachingModel<TTeaching> TeachAutoRoi(HObject teachingImage, ISinglePackageTeachingModel<TTeaching> teaching, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiRight);

            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            VisionOperation.GetImageMidPoint(teachingImage, out Point midPoint);
            VisionOperation.FindMostMidRectangle(teachingImage!, midPoint, inspectedTeaching.PackageThreshold, out HObject region, out _);
            region.DisposeBy(DisposeBag);

            // region 테두리 박스 좌표 추출
            HOperatorSet.RegionFeatures(region, "row1", out HTuple row1);
            HOperatorSet.RegionFeatures(region, "column1", out HTuple column1);
            HOperatorSet.RegionFeatures(region, "row2", out HTuple row2);
            HOperatorSet.RegionFeatures(region, "column2", out HTuple column2);
            DisposeBag.Add(row1, column1, row2, column2);

            const int size = 50;

            inspectedTeaching.PackageRoiTop = new Roi("TOP", row1.D - size, column1.D + size, row1.D + size, column2.D - size);
            inspectedTeaching.PackageRoiBottom = new Roi("BOTTOM", row2.D - size, column1.D + size, row2.D + size, column2.D - size);
            inspectedTeaching.PackageRoiLeft = new Roi("LEFT", row1.D + size, column1.D - size, row2.D - size, column1.D + size);
            inspectedTeaching.PackageRoiRight = new Roi("RIGHT", row1.D + size, column2.D - size, row2.D - size, column2.D + size);

            renderData.ResultDrawings.Add((drawingObject: region, color: EColor.Orange));

            return inspectedTeaching;
        }

        public ISinglePackageTeachingModel<TTeaching> TeachAutoThreshold(HObject teachingImage, ISinglePackageTeachingModel<TTeaching> teaching, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiRight);

            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            inspectedTeaching.PackageThresholdDiff = 0;
            inspectedTeaching.PackageThresholdDiff = VisionEngine.FindPackageThresholdDiffAuto(
                image: teachingImage,
                top: inspectedTeaching.PackageRoiTop,
                bottom: inspectedTeaching.PackageRoiBottom,
                left: inspectedTeaching.PackageRoiLeft,
                right: inspectedTeaching.PackageRoiRight,
                direction: inspectedTeaching.PackageEdgeDetectDirection,
                detectMode: inspectedTeaching.PackageEdgeDetectMode);

            return inspectedTeaching;
        }

        public IPackageInspectionResultModel<TResult> InspectPackage(HObject image, ISinglePackageTeachingModel<TTeaching> teaching, ECamera camera, bool enforceAllChecks, HashSet<TItem> inspectionItems, out AlignContext alignContext, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiTop);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiBottom);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiLeft);
            ArgumentNullException.ThrowIfNull(teaching.PackageRoiRight);
            ArgumentNullException.ThrowIfNull(teaching.PackageModelRoi);

            IPackageInspectionResultModel<TResult> result = (IPackageInspectionResultModel<TResult>)new TResult();
            renderData = new InspectionRenderData();
            alignContext = new AlignContext();

            ArgumentNullException.ThrowIfNull(teaching.ModelHandleForAlign);
            ArgumentNullException.ThrowIfNull(teaching.HomMat2DModelForAlign);

            try
            {
                VisionEngine.AlignImage(
                    image: image,
                    modelHandle: teaching.ModelHandleForAlign,
                    homMat2DModel: teaching.HomMat2DModelForAlign,
                    center: teaching.PackageCenter,
                    out alignContext.AlignedImage,
                    out alignContext.TransformMatrix,
                    out alignContext.TransformMatrixInvert
                    );

                VisionEngine.GetPackageRegion(
                    image: alignContext.AlignedImage,
                    top: teaching.PackageRoiTop,
                    bottom: teaching.PackageRoiBottom,
                    left: teaching.PackageRoiLeft,
                    right: teaching.PackageRoiRight,
                    direction: teaching.PackageEdgeDetectDirection,
                    detectMode: teaching.PackageEdgeDetectMode,
                    thresholdDiff: teaching.PackageThresholdDiff,
                    out alignContext.PackageRegion,
                    out alignContext.PackagePoints
                    );

                renderData.AddMatrix(alignContext.TransformMatrixInvert);

                result.PackageRegion = alignContext.PackageRegion;
                result.PackagePoints = alignContext.PackagePoints;

                result.HasDevice = new Result<bool>(EResultType.Good, true);

                renderData.ResultDrawings.Add((drawingObject: alignContext.PackageRegion, color: EColor.Green));
            }
            catch
            {
                result.HasDevice = new Result<bool>(EResultType.NoDevice, false);

                return result;
            }

            if (packageItemProvider is not null)
            {
                if (inspectionItems.Contains(packageItemProvider.PackageOffset) || enforceAllChecks)
                {
                    VisionOperation.AffineTransformRegion(
                        region: alignContext.PackageRegion,
                        homMat2D: alignContext.TransformMatrixInvert,
                        out HObject invPackageRegion
                        );

                    result.PackageOffset = VisionEngine.InspectPackageOffset(
                        image: image,
                        packageRegion: invPackageRegion,
                        cameraType: camera,
                        out HObject imageReticle,
                        out HObject packageReticle,
                        out FixedText packageOffsetText
                        );

                    renderData.AddText(packageOffsetText);

                    using (imageReticle)
                    {
                        var imageReticleTrans = imageReticle
                            .AffineTransformContourXld(alignContext.TransformMatrix)
                            .DisposeBy(DisposeBag);

                        renderData.ResultDrawings.Add((drawingObject: imageReticleTrans, color: EColor.Cyan));
                    }

                    using (packageReticle)
                    {
                        var packageReticleTrans = packageReticle
                            .AffineTransformContourXld(alignContext.TransformMatrix)
                            .DisposeBy(DisposeBag);

                        renderData.ResultDrawings.Add((drawingObject: packageReticleTrans, color: result.PackageOffset.Type.GetResultColor((InspectionTeaching)teaching)));
                    }
                }

                if (inspectionItems.Contains(packageItemProvider.PackageSize) || enforceAllChecks)
                {
                    result.PackageSize = VisionEngine.InspectPackageSize(
                        packageRegion: alignContext.PackageRegion,
                        originalSize: OriginPackageSize,
                        tolerance: PackageSizeTolerance,
                        cameraType: camera,
                        out FixedText packageSizeText
                        );

                    renderData.AddText(packageSizeText);
                }
            }

            return result;
        }
    }
}
