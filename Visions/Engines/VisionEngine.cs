using System.Collections.Concurrent;
using GVisionWpf.Exceptions;
using System.Threading.Tasks;
using GVisionWpf.GlobalStates;
using KdTree;
using KdTree.Math;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace GVisionWpf.Visions.Engines
{
    public class VisionEngine
    {
        public static void OmitRegionFromTarget(HObject image, List<Roi> rois, double dilation, out HObject resultImage)
        {
            VisionOperation.Rois2UnionRegion(rois, out HObject regions);
            if (dilation > 0)
            {
                HOperatorSet.DilationCircle(regions, out regions, dilation);
            }

            VisionOperation.ReduceDomainComplement(image, regions, out resultImage);
            regions.Dispose();
        }

        public static void OmitRegionFromTarget(HObject image, HObject region, double dilation, out HObject resultImage)
        {
            HOperatorSet.DilationCircle(region, out HObject regionDilation, dilation);
            VisionOperation.ReduceDomainComplement(image, regionDilation, out resultImage);
            regionDilation.Dispose();
        }

        public static Result<double> AlignImage(HObject image, HTuple modelHandle, HTuple homMat2DModel, Pose center, out HObject alignedImage, out HTuple transformationMatrix, out HTuple transformationMatrixInvert)
        {
            HOperatorSet.FindGenericShapeModel(image, modelHandle, out HTuple matchResultId, out _);
            HOperatorSet.GetGenericShapeModelResult(matchResultId, "best", "score", out HTuple matchScore);
            HOperatorSet.GetGenericShapeModelResultObject(out HObject contours, matchResultId, "best", "contours");
            contours.Dispose();

            HOperatorSet.GetGenericShapeModelResult(matchResultId, "best", "hom_mat_2d", out HTuple homMat2DMatch);
            HOperatorSet.HomMat2dInvert(homMat2DMatch, out HTuple homeMat2DMatchInvert);
            HOperatorSet.HomMat2dCompose(homMat2DModel, homeMat2DMatchInvert, out transformationMatrix);
            HOperatorSet.HomMat2dInvert(transformationMatrix, out transformationMatrixInvert);

            VisionOperation.AffineTransformImage(image, transformationMatrix, out alignedImage);

            return new Result<double>(EResultType.Good, matchScore.D);
        }

        public static void GetPackageRegion(HObject image, Roi top, Roi bottom, Roi left, Roi right, EEdgeDetectDirection direction, EEdgeDetectMode detectMode, int thresholdDiff, out HObject region, out List<Point> points)
        {
            VisionOperation.GetFitPolygonRegionBy4Box(image, top, bottom, left, right, direction, detectMode, thresholdDiff, out region, out points);
            HOperatorSet.ErosionCircle(region, out region, 1);
        }

        public static Result<CornerDegree> InspectCornerDegree(List<Point> points, double tolerance, out HObject cornerPoint, out List<FloatingText> texts)
        {
            const int nPoints = 4;
            texts = new List<FloatingText>(4);
            List<double> degrees = new List<double>(nPoints);
            EResultType type = EResultType.Good;

            HOperatorSet.GenEmptyRegion(out cornerPoint);
            for (int i = 0; i < nPoints; i++)
            {
                HOperatorSet.AngleLl(points[(i + nPoints - 1) % nPoints].Row, points[(i + nPoints - 1) % nPoints].Col, points[i].Row, points[i].Col, points[i].Row, points[i].Col, points[(i + 1) % nPoints].Row, points[(i + 1) % nPoints].Col, out HTuple angle);

                angle = 180 + angle.TupleDeg();
                degrees.Add(angle.D);

                if (Math.Abs(angle.D - 90) > tolerance)
                {
                    type = EResultType.CornerDegree;
                }

                texts.Add(new FloatingText(angle.D.ToString("N2"), new Point(points[i].Row, points[i].Col), EColor.Green));

                HOperatorSet.GenCircle(out HObject circle, points[(i + 1) % nPoints].Row, points[(i + 1) % nPoints].Col, 3);
                HOperatorSet.ConcatObj(cornerPoint, circle, out cornerPoint);
            }

            CornerDegree cornerDegree = new CornerDegree(degrees[0], degrees[1], degrees[3], degrees[2]);
            return new Result<CornerDegree>(type, cornerDegree);
        }

        public static Result<int> InspectChipping(HObject image, HObject packageRegion, HObject dontCareRegion, Threshold threshold, double outlineWidth, double minLengthOfShortSide, double maxLengthOfShortSide, double minLengthOfLongSide, double maxLengthOfLongSide, ECamera cameraType, out HObject region)
        {
            if (outlineWidth is < 1 or > 5000)
            {
                HOperatorSet.GenEmptyRegion(out region);
                return new Result<int>();
            }

            HOperatorSet.Boundary(packageRegion, out HObject packageRegionBorder, "inner");

            HOperatorSet.DilationCircle(packageRegionBorder, out HObject inspectionRegion, outlineWidth);
            HOperatorSet.ErosionCircle(packageRegion, out HObject erodedPackageRegion, 2);
            HOperatorSet.Intersection(inspectionRegion, erodedPackageRegion, out inspectionRegion);
            erodedPackageRegion.Dispose();

            VisionOperation.ReduceDomain(image, inspectionRegion, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            HOperatorSet.Difference(inspectionRegion, region, out region);
            HOperatorSet.OpeningCircle(region, out region, 2);

            HOperatorSet.DilationCircle(dontCareRegion, out HObject dilatedDontCareRegion, 2);
            HOperatorSet.Difference(region, dilatedDontCareRegion, out region);
            dilatedDontCareRegion.Dispose();

            HOperatorSet.Connection(region, out region);

            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;
            HOperatorSet.SelectShape(region, out region, "rect2_len1", "and", unit.ConvertToPixel(cameraType, minLengthOfLongSide), unit.ConvertToPixel(cameraType, maxLengthOfLongSide));
            HOperatorSet.SelectShape(region, out region, "rect2_len2", "and", unit.ConvertToPixel(cameraType, minLengthOfShortSide), unit.ConvertToPixel(cameraType, maxLengthOfShortSide));

            VisionOperation.GetAdjacentRegions(packageRegionBorder, region, out region);

            int value = VisionOperation.GetCountOf(region);
            EResultType type = value == 0 ? EResultType.Good : EResultType.Chipping;

            return new Result<int>(type, value);
        }

        public static Result<int> InspectBurr(HObject image, HObject packageRegion, HObject dontCareRegion, Threshold threshold, double outlineWidth, double minLengthOfShortSide, double maxLengthOfShortSide, double minLengthOfLongSide, double maxLengthOfLongSide, ECamera cameraType, out HObject region)
        {
            if (outlineWidth is < 1 or > 5000)
            {
                HOperatorSet.GenEmptyRegion(out region);
                return new Result<int>();
            }

            HOperatorSet.Boundary(packageRegion, out HObject packageRegionBorder, "inner");

            HOperatorSet.DilationCircle(packageRegionBorder, out HObject inspectionRegion, outlineWidth);
            HOperatorSet.DilationCircle(packageRegion, out HObject dilatedPackageRegion, 2);
            HOperatorSet.Difference(inspectionRegion, dilatedPackageRegion, out inspectionRegion);

            VisionOperation.ReduceDomain(image, inspectionRegion, out HObject reducedImage);
            inspectionRegion.Dispose();
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();
            HOperatorSet.Difference(region, dontCareRegion, out region);

            HOperatorSet.Connection(region, out region);

            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;
            HOperatorSet.SelectShape(region, out region, "rect2_len1", "and", unit.ConvertToPixel(cameraType, minLengthOfLongSide), unit.ConvertToPixel(cameraType, maxLengthOfLongSide));
            HOperatorSet.SelectShape(region, out region, "rect2_len2", "and", unit.ConvertToPixel(cameraType, minLengthOfShortSide), unit.ConvertToPixel(cameraType, maxLengthOfShortSide));

            VisionOperation.GetAdjacentRegions(dilatedPackageRegion, region, out region);
            dilatedPackageRegion.Dispose();

            int value = VisionOperation.GetCountOf(region);
            EResultType type = value == 0 ? EResultType.Good : EResultType.Burr;

            return new Result<int>(type, value);
        }

        public static Result<Size> InspectPackageSize(HObject packageRegion, Size originalSize, Size tolerance, ECamera cameraType, out FixedText text)
        {
            VisionOperation.GetRegionOrientationOfSmallestRectangle2(packageRegion, out _, out Size pxSize);

            Size size = pxSize.ConvertFromPixel(cameraType);
            text = new FixedText($"Package Size : {size}", 5);

            // 가로 세로 고려하지 않고, 짧은 변과 긴 변을 매칭시켜 검사함
            if ((originalSize.Width > originalSize.Height && size.Width < size.Height) ||
                (originalSize.Width < originalSize.Height && size.Width > size.Height))
            {
                originalSize = new Size(originalSize.Height, originalSize.Width);
            }

            EResultType type = EResultType.Good;
            if (Math.Abs(size.Width - originalSize.Width) > tolerance.Width || Math.Abs(size.Height - originalSize.Height) > tolerance.Height)
            {
                type = EResultType.PackageSize;
            }

            return new Result<Size>(type, size);
        }

        public static Result<Pose> InspectPackageOffset(HObject image, HObject packageRegion, ECamera cameraType, out HObject imageReticle, out HObject packageReticle, out FixedText text)
        {
            VisionOperation.GetRegionOrientationOfSmallestRectangle2(packageRegion, out Pose pxPackagePose, out _);
            VisionOperation.GetImageMidPoint(image, out Point imageCenterPoint);

            Pose pxOffsetTemp = pxPackagePose - new Pose(imageCenterPoint);
            Pose pxOffset = pxPackagePose - new Pose(imageCenterPoint);
            double tRadians = pxOffset.T * Math.PI / 180.0;
            pxOffset.X = pxOffsetTemp.X * Math.Cos(tRadians) - pxOffsetTemp.Y * Math.Sin(tRadians);
            pxOffset.Y = pxOffsetTemp.X * Math.Sin(tRadians) + pxOffsetTemp.Y * Math.Cos(tRadians);
            Pose offset = pxOffset.ConvertFromPixel(cameraType);
            text = new FixedText($"Package Offset: {offset}", 4);

            double reticleSize = imageCenterPoint.Col / 30;
            VisionOperation.GenReticle(imageCenterPoint, reticleSize, out imageReticle);
            VisionOperation.GenReticle(pxPackagePose, reticleSize, out packageReticle);

            // INTENTION: Package Offset 검사는 Tolerance를 비교하지 않음
            return new Result<Pose>(EResultType.Good, offset);
        }

        public static Result<bool> InspectFirstPin(HObject image, Roi roi, Threshold threshold, EFirstPin type, out HObject firstPinRegion)
        {
            VisionOperation.ReduceDomain(image, roi, out HObject firstPinImage);
            VisionOperation.Threshold(firstPinImage, threshold, out firstPinRegion);
            firstPinImage.Dispose();

            bool isPassed;
            if (type == EFirstPin.SmallPad)
            {
                isPassed = !VisionOperation.IsEmpty(firstPinRegion);
            }
            else
            {
                isPassed = VisionOperation.IsEmpty(firstPinRegion);
            }

            return GlobalSetting.Instance.SystemType switch
            {
                ESystemType.HanaMicron => new Result<bool>(EResultType.Good, !VisionOperation.IsEmpty(firstPinRegion)),
                _ => new Result<bool>(isPassed ? EResultType.Good : EResultType.FirstPin, isPassed)
            };
        }

        public static Result<int> InspectScratch(HObject image, List<Roi> rois, Threshold threshold, int minSize, int maxSize, out HObject region)
        {
            VisionOperation.ReduceDomain(image, rois, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();

            HOperatorSet.ClosingCircle(region, out HObject closedRegion, 5);  // 커질수록 더 많은 조각이 붙음, 조각 병합
            HOperatorSet.Connection(closedRegion, out region);
            closedRegion.Dispose();

            HOperatorSet.SelectShape(region, out region, "area", "and", minSize, maxSize);
            //HOperatorSet.SelectShape(region, out region, "anisometry", "and", 2.0, 999.0); // 장 단 축 비율 

            int nScratches = VisionOperation.IsEmpty(region) ? 0 : VisionOperation.GetCountOf(region);

            return new Result<int>(nScratches == 0 ? EResultType.Good : EResultType.Scratch, nScratches);
        }

        public static Result<int> InspectForeignMaterial(HObject image, List<Roi> rois, Threshold threshold, int minSize, int maxSize, out HObject region)
        {
            // TODO: 스크래치랑 똑같은데 나중에 제대로 만들 예정
            VisionOperation.ReduceDomain(image, rois, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();
            HOperatorSet.Connection(region, out region);
            HOperatorSet.SelectShape(region, out region, "area", "and", minSize, maxSize);
            int nScratches = VisionOperation.IsEmpty(region) ? 0 : VisionOperation.GetCountOf(region);

            return new Result<int>(nScratches == 0 ? EResultType.Good : EResultType.ForeignMaterial, nScratches);
        }

        public static Result<int> InspectContamination(HObject image, List<Roi> rois, Threshold threshold, int minSize, int maxSize, out HObject region)
        {
            // TODO: 스크래치랑 똑같은데 나중에 제대로 만들 예정
            VisionOperation.ReduceDomain(image, rois, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();
            HOperatorSet.Connection(region, out region);
            HOperatorSet.SelectShape(region, out region, "area", "and", minSize, maxSize);
            int nScratches = VisionOperation.IsEmpty(region) ? 0 : VisionOperation.GetCountOf(region);

            return new Result<int>(nScratches == 0 ? EResultType.Good : EResultType.Contamination, nScratches);
        }

        public static Result<int> InspectRejectMark(HObject image, Roi roi, Threshold threshold, int minSize, int maxSize, out HObject region)
        {
            // TODO: 스크래치랑 똑같은데 나중에 제대로 만들 예정
            VisionOperation.ReduceDomain(image, roi, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();
            HOperatorSet.Connection(region, out region);
            HOperatorSet.SelectShape(region, out region, "area", "and", minSize, maxSize);

            return new Result<int>(VisionOperation.GetCountOf(region) == 0 ? EResultType.Good : EResultType.RejectMark, VisionOperation.GetCountOf(region));
        }

        public static void GetTextOfPackageNumber(Roi packageRoi, out FloatingText text, int packageNo, int fontSize = 14)
        {
            VisionOperation.Roi2Region(packageRoi, out HObject packageRegionRoi);
            VisionOperation.GetTopLeftPosition(packageRegionRoi, out Point packageTopLeftPoint);
            packageRegionRoi.Dispose();

            Point offset = new Point(5, 5);
            text = new FloatingText($"#{packageNo}", packageTopLeftPoint + offset, EColor.Green, fontSize);
        }

        public static void GetTextsOfPackageNumbers(List<Roi> packageRois, out List<FloatingText> texts, int fontSize = 14)
        {
            int nPackages = packageRois.Count;

            texts = new List<FloatingText>(nPackages);
            for (int i = 0; i < nPackages; i++)
            {
                VisionOperation.Roi2Region(packageRois[i], out HObject packageRegionRoi);
                VisionOperation.GetTopLeftPosition(packageRegionRoi, out Point packageTopLeftPoint);
                packageRegionRoi.Dispose();

                Point offset = new Point(5, 5);
                FloatingText text = new FloatingText($"#{i + 1}", packageTopLeftPoint + offset, EColor.Green, fontSize);
                texts.Add(text);
            }
        }

        public static Result<SawOffset> InspectSawOffset(List<SawOffsetItem> sawOffsetItems, List<Point> packagePoints, Dictionary<ESawOffsetStandardObject, HObject> targetDictionary, double xTolerance, double yTolerance, ECamera cameraType, out HObject perpendicularLines)
        {
            EResultType type = EResultType.Good;
            SawOffset result = new SawOffset(0, 0);
            int xCount = 0, yCount = 0;

            HOperatorSet.GenEmptyRegion(out perpendicularLines);
            foreach (SawOffsetItem sawOffsetItem in sawOffsetItems)
            {
                foreach (EDirection direction in sawOffsetItem.Directions)
                {
                    VisionOperation.GetLineFromPackagePoints(packagePoints, direction, out Point start, out Point end);

                    if (!targetDictionary.ContainsKey(sawOffsetItem.SelectedSawOffsetStandardObject))
                    {
                        result.IsExistTargetObject = false;
                        continue;
                    }

                    HOperatorSet.SelectRegionPoint(targetDictionary[sawOffsetItem.SelectedSawOffsetStandardObject], out HObject targetRegion, sawOffsetItem.SawOffsetTargetPoint.Row, sawOffsetItem.SawOffsetTargetPoint.Col);

                    if (VisionOperation.IsEmpty(targetRegion))
                    {
                        result.IsExistTargetObject = false;
                        continue;
                    }

                    try
                    {
                        VisionOperation.GetCenterPointOfRegion(targetRegion, out Point centerOfTarget);
                        VisionOperation.CalculateFootOfPerpendicular(centerOfTarget, start, end, out Point footOfPerpendicular);
                        VisionOperation.CalculateEuclideanDistance(centerOfTarget, footOfPerpendicular, out Length pxDistance);
                        HOperatorSet.GenRegionLine(out HObject perpendicularLine, centerOfTarget.Row, centerOfTarget.Col, footOfPerpendicular.Row, footOfPerpendicular.Col);
                        HOperatorSet.Union2(perpendicularLines, perpendicularLine, out perpendicularLines);
                        perpendicularLine.Dispose();

                        Length distance = pxDistance.ConvertFromPixel(cameraType);

                        Double BgaSawOffsetXStandard = GlobalSetting.Instance.Inspection.BgaSawOffsetXStandard;
                        Double BgaSawOffsetYStandard = GlobalSetting.Instance.Inspection.BgaSawOffsetYStandard;

                        Double difference;
                        double tolerance;
                        if (direction == EDirection.Left || direction == EDirection.Right)
                        {
                            result.X += distance.Value;
                            tolerance = xTolerance;
                            difference = Math.Abs(distance.Value - BgaSawOffsetXStandard);
                            xCount++;
                        }
                        else // (direction == EDirection.Top || direction == EDirection.Bottom)
                        {
                            result.Y += distance.Value;
                            tolerance = yTolerance;
                            difference = Math.Abs(distance.Value - BgaSawOffsetYStandard);
                            yCount++;
                        }

                        if (difference > tolerance)
                        {
                            type = EResultType.SawOffset;
                        }
                    }
                    catch
                    {
                        result.IsExistTargetObject = false;
                    }
                }
            }

            result.X = xCount == 0 ? 0 : result.X / xCount;
            result.Y = yCount == 0 ? 0 : result.Y / yCount;

            return new Result<SawOffset>(type, result);
        }

        public static Result<Ratio> InspectArea(HObject region, int teachingArea, int tolerance, EResultType errorType)
        {
            if (teachingArea == 0)
            {
                // 티칭 정보 없으면 검사 안 할게요.
                return new Result<Ratio>(EResultType.Good, new Ratio(0));
            }

            VisionOperation.GetAreaRatio(region, teachingArea, out Ratio ratio);
            EResultType type = Math.Abs(ratio.Value - 100) < tolerance ? EResultType.Good : errorType;

            return new Result<Ratio>(type, ratio);
        }

        // Area는 px받아서 px로 검사합니다.
        public static Result<StatisticalList<Ratio>> InspectAreas(HObject region, int avgArea, int tolerance, EResultType errorType)
        {
            EResultType type = EResultType.Good;

            StatisticalList<Ratio> ratios = new StatisticalList<Ratio>();
            for (int i = 1; i <= region.CountObj(); i++)
            {
                Result<Ratio> ratio = InspectArea(region[i], avgArea, tolerance, errorType);
                ratios.Add(ratio.Value);

                if (ratio.Type == errorType)
                {
                    type = errorType;
                }
            }

            return new Result<StatisticalList<Ratio>>(type, ratios);
        }

        public static Result<StatisticalList<Pose>> InspectOffsets(HObject region, List<Pose> centerPxPoses, Pose tolerance, ECamera cameraType, EResultType errorType, out HObject errorRegion)
        {
            HOperatorSet.GenEmptyRegion(out errorRegion);
            StatisticalList<Pose> offsets = new StatisticalList<Pose>();
            EResultType type = EResultType.Good;

            VisionOperation.GetRegionOrientationOfSmallestRectangle2(region, out List<Pose> pxPoses, out _);
            if (pxPoses.Count != centerPxPoses.Count)
            {
                return new Result<StatisticalList<Pose>>();
            }

            for (int i = 0; i < pxPoses.Count; i++)
            {
                Pose pxOffset = pxPoses[i] - centerPxPoses[i];
                Pose offset = pxOffset.ConvertFromPixel(cameraType);
                offsets.Add(offset);

                if (Math.Abs(offset.X) > tolerance.X || Math.Abs(offset.Y) > tolerance.Y || offset.T > tolerance.T)
                {
                    type = errorType;
                    HOperatorSet.ConcatObj(errorRegion, region[i + 1], out errorRegion);
                }
            }

            return new Result<StatisticalList<Pose>>(type, offsets);
        }

        public static int FindPackageThresholdDiffAuto(HObject image, Roi top, Roi bottom, Roi left, Roi right, EEdgeDetectDirection direction, EEdgeDetectMode detectMode)
        {
            List<int> plausibleThresholds = new List<int>(50);
            for (int threshold = 1; threshold < 255; threshold += 2)
            {
                try
                {
                    VisionOperation.GetFitPolygonRegionBy4Box(image, top, bottom, left, right, direction, detectMode, threshold, out HObject packageRegion, out _);
                    packageRegion.Dispose();
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
                return 0;
            }

            return plausibleThresholds.Sum() / plausibleThresholds.Count;
        }

        public static int FindGriddedPackageThresholdDiffAuto(HObject image, List<Roi> packageRois, EEdgeDetectDirection direction, EEdgeDetectMode mode)
        {
            ConcurrentBag<int> thresholdBag = new ConcurrentBag<int>();
            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(packageRois, parallelOptions, (roi, state) =>
            {
                try
                {
                    VisionOperation.Roi2BorderBoxes(roi, out Roi top, out Roi bottom, out Roi left, out Roi right);
                    int threshold = FindPackageThresholdDiffAuto(image, top, bottom, left, right, direction, mode);
                    thresholdBag.Add(threshold);
                }
                catch (VisionNotFoundException)
                {
                    // ignore
                }
            });

            if (thresholdBag.IsEmpty)
            {
                return 0;
            }

            // threshold의 최빈값을 선택
            return thresholdBag
                .Where(t => t > 0)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .Select(g => g.Key)
                .DefaultIfEmpty(0)
                .First();
        }

        public static Result<int> InspectPattern(HObject image, List<Rect> patternRects, Threshold threshold, out HObject patternRegion)
        {
            VisionOperation.Rects2Rois(patternRects, out List<Roi> patterns);
            VisionOperation.ReduceDomain(image, patterns, out HObject patternImage);
            VisionOperation.Threshold(patternImage, threshold, out patternRegion);
            HOperatorSet.Connection(patternRegion, out patternRegion);
            patternImage.Dispose();

            // INTENTION: 패턴은 불량 여부를 검사하지 않고 리전만 추출함
            return new Result<int>
            {
                Value = VisionOperation.GetCountOf(patternRegion),
                Type = EResultType.Good
            };
        }
        public static void Rois2UnionRegion(List<Roi> rois, out HObject rectangle)
        {
            HOperatorSet.GenEmptyRegion(out rectangle);
            foreach (Roi roi in rois)
            {
                HObject region;
                HOperatorSet.GenRectangle1(out region, roi.Row1, roi.Col1, roi.Row2, roi.Col2);
                HOperatorSet.Union2(rectangle, region, out rectangle);
            }
        }

        public static Result<StatisticalList<Length>> InspectPitch(List<Roi> rois, HObject regions, Dictionary<string, Length> expectedPitchByRoi, Length tolerance, ECamera cameraType, EResultType errorType, out HObject wrongPitchRegions, out HObject edgeRegion, double minAngle = 20.0, double maxAngle = 70.0)
        {
            EResultType type = EResultType.Good;
            StatisticalList<Length> pitches = new StatisticalList<Length>();
            HOperatorSet.GenEmptyRegion(out edgeRegion);
            HOperatorSet.GenEmptyRegion(out wrongPitchRegions);

            foreach (Roi roi in rois)
            {
                // todo: region으로 하지 말고 circle dict 사용
                VisionOperation.Roi2Region(roi, out HObject roiRegion);
                HOperatorSet.Intersection(roiRegion, regions, out HObject segmentedRegion);
                roiRegion.Dispose();
                HOperatorSet.Connection(segmentedRegion, out HObject connectedRegions);
                segmentedRegion.Dispose();

                VisionOperation.GetCenterPointsOfRegions(connectedRegions, out List<Point> points);
                connectedRegions.Dispose();

                if (!points.Any())
                {
                    continue;
                }

                List<Line> edges;
                try
                {
                    VisionOperation.ComputeDelaunayEdges(points, out edges);
                }
                catch (Exception ex)
                {
                    continue;
                }

                KdTree<double, Point> kdTree = new KdTree<double, Point>(2, new DoubleMath());
                points.ForEach(point => kdTree.Add(new[] { point.Row, point.Col }, point));

                foreach (Line edge in edges)
                {
                    if (VisionOperation.HasIntermediatePoint(edge, kdTree, 10.0))
                    {
                        continue;
                    }

                    double angle = Math.Abs(VisionOperation.CalculateLineAngle(edge) % 90);
                    if (angle > minAngle && angle < maxAngle)
                    {
                        continue;
                    }

                    VisionOperation.CalculateEuclideanDistance(edge.Start, edge.End, out Length pxPitch);
                    Length pitch = pxPitch.ConvertFromPixel(cameraType);

                    Length expectedPitch = expectedPitchByRoi[roi.Name];
                    if (pitch > expectedPitch * 1.5)
                    {
                        continue;
                    }

                    HOperatorSet.GenRegionLine(out HObject line, edge.Start.Row, edge.Start.Col, edge.End.Row, edge.End.Col);
                    HOperatorSet.ConcatObj(edgeRegion, line, out edgeRegion);

                    if ((pitch - expectedPitch).Abs() > tolerance)
                    {
                        type = errorType;
                        HOperatorSet.ConcatObj(wrongPitchRegions, line, out wrongPitchRegions);
                    }

                    pitches.Add(pitch);
                    line.Dispose();
                }
            }

            return new Result<StatisticalList<Length>>(type, pitches);
        }

       }
}