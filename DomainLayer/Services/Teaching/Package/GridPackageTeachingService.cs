using System.Collections.Generic;
using ControlzEx.Standard;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Exceptions;
using GVisionWpf.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.Package
{
    public sealed partial class GridPackageTeachingService<TTeaching, TResult, TItem> : IGridPackageTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IPackageItemProvider<TItem>? packageItemProvider = PackageItemProviderFactory.GetProvider<TItem>();
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public IGridPackageTeachingModel<TTeaching> TeachAutoThreshold(HObject teachingImage, IGridPackageTeachingModel<TTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoi);

            var inspectedTeaching = DeepCopy.Copy(teaching);

            VisionOperation.PartitionRectangle(inspectedTeaching.PackageRoi, inspectedTeaching.RowSize, inspectedTeaching.ColumnSize, out HObject partition);
            partition.DisposeBy(DisposeBag);

            List<Roi> rois = partition.Region2Rois();

            List<int> plausibleThresholds = new List<int>(50);
            foreach (var roi in rois)
            {
                try
                {
                    VisionOperation.Roi2BorderBoxes(roi: roi, out Roi top, out Roi bottom, out Roi left, out Roi right);
                    int threshold = VisionEngine.FindPackageThresholdDiffAuto(teachingImage, top, bottom, left, right,
                        inspectedTeaching.PackageEdgeDetectDirection, inspectedTeaching.PackageEdgeDetectMode);

                    plausibleThresholds.Add(threshold);
                }
                catch (VisionNotFoundException)
                {
                    if (plausibleThresholds.Count > 0)
                    {
                        break;
                    }
                }
            }

            if (plausibleThresholds.Count == 0)
            {
                inspectedTeaching.PackageThresholdDiff = 0;
                return inspectedTeaching;
            }

            inspectedTeaching.PackageThresholdDiff = plausibleThresholds.Sum() / plausibleThresholds.Count;
            return inspectedTeaching;
        }

        public IEnumerable<Roi> PartitionRoi(IGridPackageTeachingModel<TTeaching> teaching)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoi);

            MapEngine.PartitionRoi(teaching.PackageRoi, teaching.RowSize, teaching.ColumnSize, out List<Roi> packageRois);
            return packageRois;
        }

        public IPackageInspectionResultModel<TResult> InspectSinglePackage(HObject image, IGridPackageTeachingModel<TTeaching> teaching, Roi packageRoi, int pakcageIndex, ECamera camera, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoi);

            IPackageInspectionResultModel<TResult> result = (IPackageInspectionResultModel<TResult>)new TResult();

            renderData = new InspectionRenderData();

            List<Point> packagePoints;
            HObject packageRegion;
            EColor packageOutlineColor = EColor.Green;

            try
            {
                VisionOperation.Roi2BorderBoxes(packageRoi, out Roi top, out Roi bottom, out Roi left, out Roi right);

                VisionEngine.GetPackageRegion(
                    image: image,
                    top: top, bottom: bottom, left: left, right: right,
                    direction: teaching.PackageEdgeDetectDirection,
                    detectMode: teaching.PackageEdgeDetectMode,
                    thresholdDiff: teaching.PackageThresholdDiff,
                    out packageRegion,
                    out packagePoints
                    );

                packageRegion.DisposeBy(DisposeBag);

                result.PackageRegion = packageRegion;
                result.PackagePoints = packagePoints;

                result.HasDevice = new Result<bool>(EResultType.Good, true);
            }
            catch
            {
                HObject region = packageRoi
                    .Roi2Region()
                    .DisposeBy(DisposeBag);

                result.HasDevice = new Result<bool>(EResultType.NoDevice, false);

                renderData.ResultDrawings.Add((drawingObject: region, color: EResultType.NoDevice.GetResultColor((InspectionTeaching)teaching)));

                return result;
            }

            if (packageItemProvider is not null)
            {
                if (inspectionItems.Contains(packageItemProvider.PackageOffset) || enforceAllChecks)
                {
                    var packageOffset = VisionEngine.InspectPackageOffset(image, packageRegion, camera, out HObject imageReticle, out HObject packageReticle, out FixedText packageOffsetText);
                    result.PackageOffset = packageOffset;

                    var drawingColor = packageOffset.Type != EResultType.Good ? EColor.Green : EResultType.PackageOffset.GetResultColor((InspectionTeaching)teaching); ;
                    renderData.ResultDrawings.Add((drawingObject: packageReticle, color: drawingColor));

                    imageReticle.DisposeBy(DisposeBag);
                    packageReticle.DisposeBy(DisposeBag);

                }

                if (inspectionItems.Contains(packageItemProvider.PackageSize) || enforceAllChecks)
                {
                    var packageSize = MapEngine.InspectPackageSize(packageRegion, out FixedText packageSizeText);
                    result.PackageSize = packageSize;

                    if (result.PackageSize.Type != EResultType.Good)
                    {
                        packageOutlineColor = EResultType.PackageSize.GetResultColor((InspectionTeaching)teaching);
                    }
                }

                VisionEngine.GetTextOfPackageNumber(packageRoi, out FloatingText packageNoText, pakcageIndex + 1, 14);

                renderData.AddText(packageNoText);

                renderData.ResultDrawings.Add((drawingObject: packageRegion, packageOutlineColor));
            }

            return result;
        }

        public IEnumerable<IPackageInspectionResultModel<TResult>> InspectGridPackages(HObject teachingImage, IGridPackageTeachingModel<TTeaching> teaching, ECamera camera, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            ArgumentNullException.ThrowIfNull(teaching.PackageRoi);

            List<IPackageInspectionResultModel<TResult>> results = new List<IPackageInspectionResultModel<TResult>>();

            renderData = new InspectionRenderData();

            List<Roi> packageRois = PartitionRoi(teaching).ToList();

            for (int packageIndex = 0; packageIndex < packageRois.Count; packageIndex++)
            {
                var result = InspectSinglePackage(teachingImage, teaching, packageRois[packageIndex], packageIndex, camera, enforceAllChecks, inspectionItems, out InspectionRenderData packageRender);

                results.Add(result);

                renderData.MergeWith(packageRender);
            }

            return results;
        }
    }
}
