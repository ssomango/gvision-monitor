using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Mark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Mark;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.DomainLayer.Services.Teaching.Mark
{
    public sealed partial class MarkTeachingService<TTeaching, TResult, TItem> : IMarkTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        private IMarkItemProvider<TItem>? markItemProvider = MarkProviderFactory.GetProvider<TItem>();
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        private HObject preprocessInspectionImage(AlignContext alignContext, TTeaching teaching)
        {
            HObject inspectionImage;

            if (teaching is ISinglePackageTeachingModel<TTeaching> singlePackageTeaching)
            {
                HOperatorSet.ErosionCircle(alignContext.PackageRegion, out HObject erosionPackageRegion, 2);
                VisionOperation.ReduceDomain(alignContext.AlignedImage, erosionPackageRegion, out inspectionImage);
                erosionPackageRegion.Dispose();
            }
            else if (teaching is IGridPackageTeachingModel<TTeaching> gridPackageTeaching)
            {
                inspectionImage = alignContext.AlignedImage;
            }
            else
            {
                throw new Exception($"Unsupported teaching model type: {teaching.GetType()}");
            }

            // Omit DontCare
            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching
                && !dontCareTeaching.DontCareRois.IsNullOrEmpty())
            {
                inspectionImage = inspectionImage.OmitRegionFromTarget(dontCareTeaching.DontCareRois.ToList(), 2);
            }

            return inspectionImage;
        }

        public IMarkTeachingModel<TTeaching> TeachMarks(HObject teachingImage, HObject packageRegion, IMarkTeachingModel<TTeaching> teaching, IDontCareTeachingModel<TTeaching>? dontcare, out InspectionRenderData renderData)
        {
            var inspectedTeaching = DeepCopy.Copy(teaching);
            renderData = new InspectionRenderData();

            HOperatorSet.GenEmptyRegion(out HObject markRegions);
            markRegions.DisposeBy(DisposeBag);

            List<MarkItem> tmp = new List<MarkItem>();

            foreach (MarkItem markItem in inspectedTeaching.MarkItems)
            {
                VisionOperation.ReduceDomain(teachingImage, markItem.Roi, out HObject markImage);

                using (markImage)
                {
                    HObject connectedRegion = VisionOperation.GetConnectedTextRegion(markImage, inspectedTeaching.MarkThreshold);

                    markItem.connectedTextRegion = connectedRegion;

                    markItem.nCharacters = VisionOperation.GetCountOf(connectedRegion);

                    switch (markItem.Mode)
                    {
                        case EMarkMode.ShapeMatching:
                            var matchingImage = markImage
                                .CropDomain()
                                .DisposeBy(DisposeBag);

                            markItem.ShapeMatchingModel = MapEngine.TrainMarkPattern(matchingImage, inspectedTeaching.MarkThreshold);
                            break;

                        case EMarkMode.Ocr:
                            HOperatorSet.ReadOcrClassCnn("Universal_0-9A-Z_NoRej.occ", out HTuple ocrHandle);
                            markItem.OcrText = VisionOperation.GetOcredText(teachingImage, connectedRegion, ".*", ocrHandle);

                            ocrHandle.ClearHandle();
                            ocrHandle.Dispose();
                            break;

                        default:
                            break;
                    }

                    HOperatorSet.Union2(markRegions, connectedRegion, out markRegions);
                    markRegions.DisposeBy(DisposeBag);

                    renderData.ResultDrawings.Add((drawingObject: markRegions, color: EColor.Green));

                    tmp.Add(markItem);
                }
            }

            inspectedTeaching.MarkItems = tmp;

            VisionOperation.GetRegionOrientationOfSmallestRectangle2(packageRegion, out Pose packagePose, out _);
            VisionOperation.GetRegionOrientationOfSmallestRectangle2(markRegions, out Pose markPose, out _);

            Roi smallestMarkRegionRoi = markRegions
                .Region2Roi();

            HObject smallestMarkRegion = smallestMarkRegionRoi
                .Roi2Region()
                .DisposeBy(DisposeBag);

            renderData.ResultDrawings.Add((drawingObject: smallestMarkRegion, color: EColor.Orange));

            VisionOperation.GetCenterPointOfRegion(teachingImage, out Point imageCenterPoint);
            double reticleSize = imageCenterPoint.Col / 30;

            VisionOperation.GenReticle(packagePose, reticleSize, out HObject packageReticle);
            renderData.ResultDrawings.Add((drawingObject: packageReticle, color: EColor.Green));
            packageReticle.DisposeBy(DisposeBag);

            VisionOperation.GenReticle(markPose, reticleSize, out HObject markReticle);
            renderData.ResultDrawings.Add((drawingObject: markReticle, color: EColor.Orange));
            markReticle.DisposeBy(DisposeBag);

            Pose pxTextOffset = markPose - packagePose;
            inspectedTeaching.TextOffset = pxTextOffset;

            return inspectedTeaching;
        }

        public IMarkInspectionResultModel<TResult> InspectMarks(AlignContext alignContext, IMarkTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {
            IMarkInspectionResultModel<TResult> result = (IMarkInspectionResultModel<TResult>)new TResult();

            renderData = new InspectionRenderData();

            var inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching);

            var copyTeaching = DeepCopy.Copy(teaching);

            HOperatorSet.GenEmptyRegion(out HObject markRegions);
            markRegions.DisposeBy(DisposeBag);

            foreach (MarkItem mark in copyTeaching.MarkItems)
            {
                VisionOperation.ReduceDomain(inspectionImage, mark.Roi, out HObject markImage);

                using (markImage)
                {
                    HObject connectedTextRegion = VisionOperation.GetConnectedTextRegion(markImage, copyTeaching.MarkThreshold);

                    mark.connectedTextRegion = connectedTextRegion;

                    HOperatorSet.DilationCircle(mark.connectedTextRegion, out HObject dilatedRegions, 7);

                    using (dilatedRegions)
                    {
                        HOperatorSet.Union2(markRegions, dilatedRegions, out markRegions);
                    }
                }
            }


            if ((markItemProvider is not null && inspectionItems.Contains(markItemProvider.WrongMark)) || enforceAllChecks)
            {
                string resultTexts = string.Empty;
                int errorCount = 0;

                foreach (MarkItem markItem in copyTeaching.MarkItems)
                {
                    VisionOperation.ReduceDomain(inspectionImage, markItem.Roi, out HObject markImage);

                    using (markImage)
                    {
                        bool isMarkPassed = false;

                        switch (markItem.Mode)
                        {
                            case EMarkMode.Ocr:
                                HOperatorSet.ReadOcrClassCnn("Universal_0-9A-Z_NoRej.occ", out HTuple ocrHandle);
                                markItem.OcrText = VisionOperation.GetOcredText(inspectionImage, markItem.connectedTextRegion, ".*", ocrHandle);

                                using (ocrHandle)
                                {
                                    isMarkPassed = MapEngine.InspectOcrMark(markImage, markItem, ocrHandle, out string ocredText);
                                    resultTexts += ocredText + "\n";
                                    ocrHandle.ClearHandle();
                                }


                                break;

                            case EMarkMode.ShapeMatching:
                                isMarkPassed = MapEngine.InspectMatchingMark(markImage, markItem, out double matchingRate);
                                resultTexts += "Match: " + matchingRate + "%\n";
                                break;

                            default:
                                break;
                        }

                        if (isMarkPassed)
                        {
                            var region = markItem.connectedTextRegion
                                .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                .DisposeBy(DisposeBag);

                            renderData.ResultDrawings.Add((drawingObject: region, color: EResultType.Good.GetResultColor((InspectionTeaching)copyTeaching)));
                        }
                        else
                        {
                            if (!VisionOperation.IsEmpty(markItem.connectedTextRegion)) // (markItem.connectedTextRegion.IsEmpty(disposeBag: DisposeBag))
                            {

                            }
                            else
                            {
                                var wrongMarkRegionTrans = markItem.connectedTextRegion
                                .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                .DisposeBy(DisposeBag);

                                renderData.ResultDrawings.Add((drawingObject: wrongMarkRegionTrans, EResultType.WrongMark.GetResultColor((InspectionTeaching)copyTeaching)));
                            }

                            errorCount++;
                        }
                    }
                }

                if (errorCount > 0)
                {
                    result.Mark = new Result<string>(EResultType.WrongMark, resultTexts);
                }
                else
                {
                    result.Mark = new Result<string>(EResultType.Good, resultTexts);
                }
            }

            if ((markItemProvider is not null && inspectionItems.Contains(markItemProvider.MissingChar))
                || (markItemProvider is not null && inspectionItems.Contains(markItemProvider.NoMark))
                || enforceAllChecks)
            {
                int missingCount = 0;
                int noMarkCount = 0;

                foreach (MarkItem markItem in copyTeaching.MarkItems)
                {
                    int nCharacters = MapEngine.CountCharacter(markItem, out HObject textBoxes);

                    using (textBoxes)
                    {
                        if ((markItemProvider is not null && inspectionItems.Contains(markItemProvider.MissingChar)) || enforceAllChecks)
                        {
                            if (nCharacters != markItem.nCharacters)
                            {
                                missingCount++;

                                HObject errorRoiRegion = markItem.Roi
                                    .Roi2Region()
                                    .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                    .DisposeBy(DisposeBag);

                                HObject textBoxesRegionTrans = textBoxes
                                    .AffineTransformRegion(alignContext.TransformMatrixInvert)
                                    .DisposeBy(DisposeBag);

                                renderData.ResultDrawings.Add((drawingObject: errorRoiRegion, color: EResultType.MissingChar.GetResultColor((InspectionTeaching)copyTeaching)));
                                renderData.ResultDrawings.Add((drawingObject: textBoxesRegionTrans, color: EResultType.MissingChar.GetResultColor((InspectionTeaching)copyTeaching)));
                            }
                        }

                        if ((markItemProvider is not null && inspectionItems.Contains(markItemProvider.NoMark)) || enforceAllChecks)
                        {
                            if (nCharacters == 0)
                            {
                                noMarkCount++;

                                HObject errorRoiRegion = markItem.Roi
                                    .Roi2Region()
                                    .AffineTransformImage(alignContext.TransformMatrixInvert)
                                    .DisposeBy(DisposeBag);

                                renderData.ResultDrawings.Add((drawingObject: errorRoiRegion, color: EResultType.NoMark.GetResultColor((InspectionTeaching)copyTeaching)));
                            }
                        }
                    }
                }

                if (missingCount > 0)
                {
                    result.MissingCharacter = new Result<int>(EResultType.MarkCount, missingCount);
                }
                else
                {
                    result.MissingCharacter = new Result<int>(EResultType.Good, 0);
                }

                if (noMarkCount > 0)
                {
                    result.NoMark = new Result<int>(EResultType.NoMark, noMarkCount);
                }
                else
                {
                    result.NoMark = new Result<int>(EResultType.Good, 0);
                }
            }

            if ((markItemProvider is not null && inspectionItems.Contains(markItemProvider.TextOffset)) || enforceAllChecks)
            {
                result.TextOffset = MapEngine.InspectTextOffset(((IBasePackageModel<TTeaching>)copyTeaching).PackageCenter, copyTeaching.MarkItems, copyTeaching.TextOffset);
            }

            return result;
        }
    }
}
