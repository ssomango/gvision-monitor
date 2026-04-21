using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using DelaunatorSharp;
using GVisionWpf.Exceptions;
using GVisionWpf.Models.Visions;
using GVisionWpf.Types;
using HalconDotNet;
using KdTree;
using Point = GVisionWpf.Models.Visions.Point;
using Rect = GVisionWpf.Models.Visions.Rect;
using Size = GVisionWpf.Models.Visions.Size;

namespace GVisionWpf.Visions
{
    public class VisionOperation
    {
        public static void CropImage(HObject image, double width, double height, out HObject croppedImage)
        {
            GetHObjectSize(image, out double originWidth, out double originHeight, out _);

            if (originWidth < width || originHeight < height)
            {
                throw new WrongValueException();
            }

            double horizontalDiff = originWidth - width;
            double verticalDiff = originHeight - height;
            Roi roi = new Roi("", verticalDiff / 2, horizontalDiff / 2, originHeight - verticalDiff / 2, originWidth - horizontalDiff / 2);

            CropImage(image, roi, out croppedImage);
        }

        public static void CropImage(HObject image, HObject region, out HObject croppedImage)
        {
            VisionOperation.ReduceDomain(image, region, out croppedImage);
            HOperatorSet.CropDomain(croppedImage, out croppedImage);
        }

        public static void CropImage(HObject image, Roi roi, out HObject croppedImage)
        {
            ReduceDomain(image, roi, out croppedImage);
            HOperatorSet.CropDomain(croppedImage, out croppedImage);
        }

        public static void GenReticle(Point point, double size, out HObject reticle)
        {
            HOperatorSet.GenCrossContourXld(out reticle, point.Row, point.Col, size, 0);
        }

        public static void GenReticle(Pose pose, double size, out HObject reticle)
        {
            HOperatorSet.GenCrossContourXld(out reticle, pose.Y, pose.X, size, new HTuple(pose.T).TupleRad());
        }

        public static void GetRegionOrientationOfSmallestRectangle2(HObject region, out List<Pose> poses, out List<Size> sizes)
        {
            poses = new List<Pose>(32);
            sizes = new List<Size>(32);

            HOperatorSet.SortRegion(region, out region, "character", "true", "row");
            for (int i = 1; i <= region.CountObj(); i++)
            {
                GetRegionOrientationOfSmallestRectangle2(region[i], out Pose pose, out Size size);
                poses.Add(pose);
                sizes.Add(size);
            }
        }

        public static void GetRegionOrientationOfSmallestRectangle2(HObject region, out Pose pxPose, out Size pxSize)
        {
            HOperatorSet.SmallestRectangle2(region, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
            HOperatorSet.TupleDeg(phi, out HTuple angle);

            if (Math.Abs(angle.D) < 45)
            {
                pxSize = new Size(length1 * 2, length2 * 2);
            }
            else
            {
                pxSize = new Size(length2 * 2, length1 * 2);
            }

            if (angle > 45)
            {
                angle -= 90;
            }

            if (angle < -45)
            {
                angle += 90;
            }

            pxPose = new Pose(column.D, row.D, angle.D);
        }

        public static void PartitionRectangle(Roi roi, int nRow, int nCol, out HObject partition)
        {
            double width = (roi.Col2 - roi.Col1) / nCol;
            double height = (roi.Row2 - roi.Row1) / nRow;

            HOperatorSet.GenRectangle1(out HObject rectangle, roi.Row1, roi.Col1, roi.Row2, roi.Col2);
            try
            {
                HOperatorSet.PartitionRectangle(rectangle, out partition, width, height);
            }
            catch
            {
                HOperatorSet.GenEmptyRegion(out partition);
            }

            rectangle.Dispose();
        }

        public static void Roi2BorderBoxes(Roi roi, out Roi top, out Roi bottom, out Roi left, out Roi right)
        {
            const int size = 50;
            top = new Roi("TOP", roi.Row1 - size, roi.Col1 + size, roi.Row1 + size, roi.Col2 - size);
            bottom = new Roi("BOTTOM", roi.Row2 - size, roi.Col1 + size, roi.Row2 + size, roi.Col2 - size);
            left = new Roi("LEFT", roi.Row1 + size, roi.Col1 - size, roi.Row2 - size, roi.Col1 + size);
            right = new Roi("RIGHT", roi.Row1 + size, roi.Col2 - size, roi.Row2 - size, roi.Col2 + size);
        }

        public static void GetFitPolygonRegionBy4Box(HObject image, Roi top, Roi bottom, Roi left, Roi right, EEdgeDetectDirection direction, EEdgeDetectMode detectMode, int thresholdDiff, out HObject region, out List<Point> points)
        {
            try
            {
#if DEBUG
                Roi2Region(top, out HObject topRoi);
                Roi2Region(bottom, out HObject bottomRoi);
                Roi2Region(left, out HObject leftRoi);
                Roi2Region(right, out HObject rightRoi);

                HOperatorSet.GenRegionLine(out HObject leftLine, left.Row2, left.Col1, left.Row1, left.Col1);
                HOperatorSet.GenRegionLine(out HObject topLine, top.Row1, top.Col1, top.Row1, top.Col2);
                HOperatorSet.GenRegionLine(out HObject rightLine, right.Row1, right.Col2, right.Row2, right.Col2);
                HOperatorSet.GenRegionLine(out HObject bottomLine, bottom.Row2, bottom.Col2, bottom.Row2, bottom.Col1);
#endif
                HOperatorSet.CreateMetrologyModel(out HTuple metrologyHandle);

                // Metrology Line 측정 객체 추가


                string searchModeStr = (detectMode == EEdgeDetectMode.BlackToWhite) ? "positive" : "negative";
                if (direction == EEdgeDetectDirection.OutToIn)
                {
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, left.Row2, (left.Col1 + left.Col2) / 2, left.Row1, (left.Col1 + left.Col2) / 2, (left.Col2 - left.Col1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, (top.Row1 + top.Row2) / 2, top.Col1, (top.Row1 + top.Row2) / 2, top.Col2, (top.Row2 - top.Row1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, right.Row1, (right.Col1 + right.Col2) / 2, right.Row2, (right.Col1 + right.Col2) / 2, (right.Col2 - right.Col1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, (bottom.Row1 + bottom.Row2) / 2, bottom.Col2, (bottom.Row1 + bottom.Row2) / 2, bottom.Col1, (bottom.Row2 - bottom.Row1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                }
                else
                {
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, left.Row1, (left.Col1 + left.Col2) / 2, left.Row2, (left.Col1 + left.Col2) / 2, (left.Col2 - left.Col1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, (top.Row1 + top.Row2) / 2, top.Col2, (top.Row1 + top.Row2) / 2, top.Col1, (top.Row2 - top.Row1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, right.Row2, (right.Col1 + right.Col2) / 2, right.Row1, (right.Col1 + right.Col2) / 2, (right.Col2 - right.Col1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                    HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, (bottom.Row1 + bottom.Row2) / 2, bottom.Col1, (bottom.Row1 + bottom.Row2) / 2, bottom.Col2, (bottom.Row2 - bottom.Row1) / 2, 10, 1.5, thresholdDiff, new HTuple("measure_transition", "measure_select"), new HTuple(searchModeStr, "first"), out _);
                }

                HOperatorSet.ApplyMetrologyModel(image, metrologyHandle);
#if DEBUG
                HOperatorSet.GetMetrologyObjectMeasures(out HObject contours, metrologyHandle, "all", "all", out _, out _);
                HOperatorSet.GetMetrologyObjectResultContour(out HObject lines, metrologyHandle, "all", "all", 1.5);
#endif
                // 측정된 직선 추출 및 교차점 계산
                HOperatorSet.GetMetrologyObjectResult(metrologyHandle, "all", "all", "result_type", "all_param", out HTuple parameters);
                HOperatorSet.ClearMetrologyObject(metrologyHandle, "all");

                //  네 라인의 교차점을 구해서 사각형의 꼭짓점 계산
                HTuple row1, row2, row3, row4, column1, column2, column3, column4;
                HOperatorSet.IntersectionLines(parameters.TupleSelect(0), parameters.TupleSelect(1), parameters.TupleSelect(2), parameters.TupleSelect(3), parameters.TupleSelect(4), parameters.TupleSelect(5), parameters.TupleSelect(6), parameters.TupleSelect(7), out row1, out column1, out _);
                HOperatorSet.IntersectionLines(parameters.TupleSelect(4), parameters.TupleSelect(5), parameters.TupleSelect(6), parameters.TupleSelect(7), parameters.TupleSelect(8), parameters.TupleSelect(9), parameters.TupleSelect(10), parameters.TupleSelect(11), out row2, out column2, out _);
                HOperatorSet.IntersectionLines(parameters.TupleSelect(8), parameters.TupleSelect(9), parameters.TupleSelect(10), parameters.TupleSelect(11), parameters.TupleSelect(12), parameters.TupleSelect(13), parameters.TupleSelect(14), parameters.TupleSelect(15), out row3, out column3, out _);
                HOperatorSet.IntersectionLines(parameters.TupleSelect(12), parameters.TupleSelect(13), parameters.TupleSelect(14), parameters.TupleSelect(15), parameters.TupleSelect(0), parameters.TupleSelect(1), parameters.TupleSelect(2), parameters.TupleSelect(3), out row4, out column4, out _);

                // 사각형 Region 생성
                HTuple rows = new HTuple(row1, row2, row3, row4, row1);
                HTuple cols = new HTuple(column1, column2, column3, column4, column1);
                HOperatorSet.GenRegionPolygonFilled(out region, rows, cols);

                // 최종 꼭짓점 정보를 List<Point>로 변환
                RowsCols2Points(rows, cols, out points);
            }
            catch
            {
                throw new VisionNotFoundException();
            }
        }

        public static void RowsCols2Points(HTuple rows, HTuple cols, out List<Point> points)
        {
            int length = Math.Min(rows.Length, cols.Length);
            points = new List<Point>(length);
            for (int i = 0; i < length; i++)
            {
                points.Add(new Point(rows[i].D, cols[i].D));
            }
        }

        public static void Region2Roi(HObject region, out Roi roi)
        {
            HOperatorSet.SmallestRectangle1(region, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
            roi = new Roi("", row1, col1, row2, col2);
        }

        public static void Region2Rois(HObject region, out List<Roi> rois)
        {
            rois = new List<Roi>(256);
            HOperatorSet.Connection(region, out region);
            for (int i = 1; i <= region.CountObj(); i++)
            {
                HOperatorSet.SelectObj(region, out HObject selectedRegion, i);

                Region2Roi(selectedRegion, out Roi roi);
                rois.Add(roi);
            }
        }

        public static void Regions2Rois(List<HObject> regions, out List<Roi> rois)
        {
            rois = new List<Roi>(256);
            foreach (HObject region in regions)
            {
                Region2Roi(region, out Roi roi);
                rois.Add(roi);
            }
        }

        public static List<Circle> BallRegions2Circles(HObject ballRegion)
        {
            const int ballListInitialSize = 512;
            List<Circle> result = new List<Circle>(ballListInitialSize);

            for (int index = 1; index <= ballRegion.CountObj(); index++)
            {
                HObject ball;
                HTuple row, col, diameter;

                HOperatorSet.SelectObj(ballRegion, out ball, index);
                HOperatorSet.RegionFeatures(ball, "row", out row);
                HOperatorSet.RegionFeatures(ball, "column", out col);
                HOperatorSet.RegionFeatures(ball, "max_diameter", out diameter);
                ball.Dispose();

                result.Add(new Circle(col, row, diameter / 2));
            }

            return result;
        }

        public static List<Point> Regions2Points(HObject regions, HTuple tolerence)
        {
            List<Point> result = new List<Point>();

            for (int index = 1; index <= regions.CountObj(); index++)
            {
                HObject region;
                HTuple row, col;

                HOperatorSet.SelectObj(regions, out region, index);
                HOperatorSet.GetRegionPolygon(region, tolerence, out row, out col);

                result.Add(new Point(row, col));
            }

            return result;
        }

        public static void GetSupersetOfSubset(HObject regions1, HObject regions2, out HObject resultRegion)
        {
            HObject regionSuperset;
            HOperatorSet.Union2(regions1, regions2, out regionSuperset);
            HOperatorSet.Connection(regionSuperset, out regionSuperset);

            HObject regionIntersection;
            HOperatorSet.Intersection(regions1, regions2, out regionIntersection);
            HOperatorSet.Union1(regionIntersection, out regionIntersection);
            HOperatorSet.Connection(regionIntersection, out regionIntersection);

            HOperatorSet.GenEmptyRegion(out resultRegion);
            for (int index = 1; index <= regionIntersection.CountObj(); index++)
            {
                HObject regionSelected;
                HOperatorSet.SelectObj(regionIntersection, out regionSelected, index);

                HTuple rows, cols;
                HOperatorSet.GetRegionPolygon(regionSelected, 99999999999999, out rows, out cols);

                if (rows.Length == 0)
                {
                    return;
                }

                HOperatorSet.SelectRegionPoint(regionSuperset, out regionSelected, rows.TupleSelect(0),
                    cols.TupleSelect(0));
                HOperatorSet.Union2(resultRegion, regionSelected, out resultRegion);
            }

            HOperatorSet.Connection(resultRegion, out resultRegion);
        }

        public static bool IsEmpty(HObject obj)
        {
            HOperatorSet.Union1(obj, out HObject objUnion);
            HOperatorSet.RegionFeatures(objUnion, "area", out HTuple value);
            objUnion.Dispose();
            if (value == null || value <= 0 || value.Type == HTupleType.EMPTY)
            {
                return true;
            }

            return value <= 0;
        }

        public static void GenCrossLineByPoint(Point point, double size, out HObject crossLine)
        {
            HObject line1, line2;
            HTuple row1 = point.Row - size / 2;
            HTuple col1 = point.Col - size / 2;
            HTuple row2 = point.Row + size / 2;
            HTuple col2 = point.Col + size / 2;

            HOperatorSet.GenRegionLine(out line1, row1, col1, row2, col2);
            HOperatorSet.GenRegionLine(out line2, row2, col1, row1, col2);
            HOperatorSet.Union2(line1, line2, out crossLine);
            HOperatorSet.DilationCircle(crossLine, out crossLine, 1);
        }

        public static void GetBallRegionByThreshold(HObject image, Threshold threshold, double minCircularity,
            out HObject ballRegion)
        {
            Threshold(image, threshold, out ballRegion);

            HOperatorSet.ClosingCircle(ballRegion, out ballRegion, 2);
            HOperatorSet.Connection(ballRegion, out ballRegion);

            HOperatorSet.SelectShape(ballRegion, out ballRegion, "circularity", "and", minCircularity, 1);
        }

        public static void Roi2Region(Roi roi, out HObject rectangle)
        {
            HOperatorSet.GenRectangle1(out rectangle, roi.Row1, roi.Col1, roi.Row2, roi.Col2);
        }

        public static void GetAlternativePackageRegion(Roi top, Roi bottom, Roi left, Roi right, out Roi roi)
        {
            roi = new Roi(
                name: "",
                row1: (top.Row1 + top.Row2) / 2,
                row2: (bottom.Row1 + bottom.Row2) / 2,
                col1: (left.Col1 + left.Col2) / 2,
                col2: (right.Col1 + right.Col2) / 2
            );
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

        public static void Rois2Regions(List<Roi> rois, out HObject rectangles)
        {
            HOperatorSet.GenEmptyRegion(out rectangles);
            foreach (Roi roi in rois)
            {
                HOperatorSet.GenRectangle1(out HObject region, roi.Row1, roi.Col1, roi.Row2, roi.Col2);
                HOperatorSet.ConcatObj(rectangles, region, out rectangles);
            }
        }

        public static HObject Region2Contour(HObject region)
        {
            HObject contour;
            try
            {
                HOperatorSet.GenContourRegionXld(region, out contour, "border");
                return contour;
            }
            catch
            {
                HOperatorSet.GenEmptyObj(out contour);
                return contour;
            }
        }

        public static void GetRegionDegreeBySmallestRect2(HObject region, out double degree)
        {
            HTuple phi;
            HOperatorSet.SmallestRectangle2(region, out _, out _, out phi, out _, out _);
            degree = phi.TupleDeg().D;
        }

        public static void GetCenterPointOfRegion(HObject region, out Point center)
        {
            HTuple row, col;
            HOperatorSet.RegionFeatures(region, "row", out row);
            HOperatorSet.RegionFeatures(region, "column", out col);
            center = new Point(row.D, col.D);
        }

        public static void GetAverageArea(HObject region, out int area)
        {
            HOperatorSet.RegionFeatures(region, "area", out HTuple pixelArea);
            area = (int)pixelArea.TupleMean().D;
        }

        public static void CreateVariationModel(HObject image, List<Roi> rois, Threshold threshold,
            out HTuple VarModelId, out HObject MinImage, out HObject MaxImage)
        {
            HTuple Width, Height;
            HObject RoiRegion,
                ReducedImage,
                MeanImage,
                ThresholdRegion,
                RegionClosing,
                ConnectedRegion,
                SelectedRegion,
                RegionUnion,
                RegionComplement,
                PaintImage,
                ImageResult,
                VarImage;
            VisionOperation.Rois2UnionRegion(rois, out RoiRegion);

            HOperatorSet.ReduceDomain(image, RoiRegion, out ReducedImage);
            HOperatorSet.GetImageSize(image, out Width, out Height);

            HOperatorSet.CreateVariationModel(Width, Height, "byte", "direct", out VarModelId);
            HOperatorSet.MeanImage(ReducedImage, out MeanImage, 7, 7);
            HOperatorSet.Threshold(MeanImage, out ThresholdRegion, threshold.MinGray, threshold.MaxGray);
            HOperatorSet.ClosingRectangle1(ThresholdRegion, out RegionClosing, 7, 7);
            HOperatorSet.Connection(RegionClosing, out ConnectedRegion);

            const int MinPix = 130;
            HOperatorSet.SelectShape(ConnectedRegion, out SelectedRegion, "area", "and", MinPix, 99999);

            HOperatorSet.Union1(SelectedRegion, out RegionUnion);
            HOperatorSet.Complement(RegionUnion, out RegionComplement);

            HOperatorSet.PaintRegion(RegionUnion, ReducedImage, out PaintImage, 255, "fill");
            HOperatorSet.PaintRegion(RegionComplement, PaintImage, out ImageResult, 0, "fill");

            HOperatorSet.BinomialFilter(ImageResult, out VarImage, 7, 7);
            HOperatorSet.PrepareDirectVariationModel(ReducedImage, VarImage, VarModelId, 50, 3.5);
            HOperatorSet.GetThreshImagesVariationModel(out MinImage, out MaxImage, VarModelId);
        }

        public static void AffineTransformImage(HObject image, HTuple homMat2D, out HObject resultImage)
        {
            HOperatorSet.AffineTransImage(image, out resultImage, homMat2D, "constant", "false");
        }

        public static void AffineTransformRegion(HObject region, HTuple homMat2D, out HObject resultRegion)
        {
            HOperatorSet.AffineTransRegion(region, out resultRegion, homMat2D, "constant");
        }

        public static void AffineTransformPoint(Point point, HTuple matrix, out Point transPoint)
        {
            HOperatorSet.AffineTransPoint2d(matrix, point.Row, point.Col, out HTuple row, out HTuple col);
            transPoint = new Point(row.D, col.D);
        }

        public static void AffineTransformPoints(List<Point> points, HTuple homMat2D, out List<Point> resultPoints)
        {
            resultPoints = new List<Point>();

            foreach (Point point in points)
            {
                AffineTransformPoint(point, homMat2D, out Point transformedPoint);
                resultPoints.Add(transformedPoint);
            }
        }

        public static void CreateShapeModelId(HObject image, HObject region, out HShapeModel shapeModel)
        {
            HOperatorSet.ReduceDomain(image, region, out HObject reducedImage);
            HImage imageModel = new HImage(reducedImage);
            reducedImage.Dispose();

            HOperatorSet.TupleRad(0, out HTuple angleStart);
            HOperatorSet.TupleRad(360, out HTuple angleEnd);

            shapeModel = imageModel.CreateShapeModel("auto", angleStart, angleEnd, "auto", "auto", "use_polarity", "auto", "auto");
            imageModel.Dispose();
        }

        // LEGACY: 옛날에 패키지 외곽 추출하던 방식
        public static void GetPackageRegionFromRoiBorders(HObject image, List<Roi> rois, Threshold threshold, out HObject packageRegion)
        {
            VisionOperation.GetImageMidPoint(image, out Point midPoint);
            VisionOperation.Rois2UnionRegion(rois, out HObject packageRoiRegion);
            VisionOperation.Region2Roi(packageRoiRegion, out Roi packageRoiUnion);
            VisionOperation.Roi2Region(packageRoiUnion, out packageRoiRegion);

            HOperatorSet.ReduceDomain(image, packageRoiRegion, out HObject packageImage);
            FindRectangle2(packageImage, midPoint, threshold, out packageRegion, out _);
        }

        public static void Threshold(HObject image, Threshold threshold, out HObject region)
        {
            if (threshold.IsAuto)
            {
                HOperatorSet.BinaryThreshold(image, out region, "max_separability", "dark", out _);
            }
            else
            {
                HOperatorSet.Threshold(image, out region, threshold.MinGray, threshold.MaxGray);
            }
        }

        public static HObject Rgb2Gray(HObject image)
        {
            HObject grayImage;
            HOperatorSet.Rgb1ToGray(image, out grayImage);
            return grayImage;
        }

        public static void EnhanceContrast(HObject chars, HObject imageInvert, out HObject imageScaleMax)
        {
            HObject regionUnion;

            HOperatorSet.GenEmptyObj(out imageScaleMax);
            HOperatorSet.GenEmptyObj(out regionUnion);
            regionUnion.Dispose();
            HOperatorSet.Union1(chars, out regionUnion);
            imageScaleMax.Dispose();
            HOperatorSet.PaintRegion(regionUnion, imageInvert, out imageScaleMax, 10, "fill");
            regionUnion.Dispose();
        }

        public static void Rois2HTuple(List<Roi> rois, out HTuple row1, out HTuple col1, out HTuple row2,
            out HTuple col2)
        {
            row1 = new HTuple();
            col1 = new HTuple();
            row2 = new HTuple();
            col2 = new HTuple();

            foreach (Roi roi in rois)
            {
                row1 = row1.TupleConcat(roi.Row1);
                col1 = col1.TupleConcat(roi.Col1);
                row2 = row2.TupleConcat(roi.Row2);
                col2 = col2.TupleConcat(roi.Col2);
            }
        }

        public static void Roi2HTuple(Roi roi, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2)
        {
            row1 = roi.Row1;
            col1 = roi.Col1;
            row2 = roi.Row2;
            col2 = roi.Col2;
        }

        public static HObject GetConnectedTextRegion(HObject image, Threshold threshold)
        {
            HObject textImage, textRegion, connectedText;

            HOperatorSet.BinomialFilter(image, out textImage, 3, 3);

            HOperatorSet.Threshold(textImage, out textRegion, threshold.MinGray, threshold.MaxGray);

            HOperatorSet.ClosingCircle(textRegion, out textRegion, 2.5);
            HOperatorSet.OpeningCircle(textRegion, out textRegion, 2.5);

            HOperatorSet.Connection(textRegion, out connectedText);

            HOperatorSet.SelectShape(connectedText, out connectedText, "area", "and", 200, 99999);
            HOperatorSet.SortRegion(connectedText, out connectedText, "character", "true", "row");

            return connectedText;
        }

        public static string GetOcredText(HObject image, HObject connectedText, HTuple regex, HTuple ocrHandle)
        {
            try
            {
                /*List<HObject> ob = new List<HObject>();
                ob.Add(TeachingImage);
                ob.Add(connectedText);
                VisionOperation.Debug(ob);*/

                HObject imageInvert;
                HTuple ocrWord;

                HOperatorSet.InvertImage(image, out imageInvert);
                // VisionOperation.EnhanceContrast(connectedText, imageInvert, out imageEnhanced);

                HOperatorSet.DoOcrWordCnn(connectedText, imageInvert, ocrHandle, regex, 2, 1, out _, out _,
                    out ocrWord, out _);
                return ocrWord;
            }
            catch
            {
                return "";
            }
        }

        public static string GetTextFromDataCode(HObject image, Roi roi, HTuple dataCodeHandle, out HObject symbolXLDs)
        {
            try
            {
                HObject codeRoi, imageReduced;
                HTuple codeRow1, codeCol1, codeRow2, codeCol2, decodedDataStrings;

                VisionOperation.Roi2HTuple(roi, out codeRow1, out codeCol1, out codeRow2, out codeCol2);
                HOperatorSet.GenRectangle1(out codeRoi, codeRow1, codeCol1, codeRow2, codeCol2);
                HOperatorSet.ReduceDomain(image, codeRoi, out imageReduced);

                HOperatorSet.FindDataCode2d(imageReduced, out symbolXLDs, dataCodeHandle, new HTuple(), new HTuple(), out _,
                    out decodedDataStrings);

                return decodedDataStrings;
            }
            catch
            {
                symbolXLDs = new HObject();
                return string.Empty;
            }
        }


        public static HObject RotateImage(HObject image, double degree)
        {
            HOperatorSet.RotateImage(image, out HObject rotatedImage, degree, "constant");

            return rotatedImage;
        }

        public static void Debug(List<HObject> objects)
        {
            HTuple handle;
            HOperatorSet.OpenWindow(0, 0, 1024, 1024, 0, "visible", "", out handle);
            HOperatorSet.SetDraw(handle, "margin");
            HOperatorSet.SetColored(handle, 12);
            HOperatorSet.SetLineWidth(handle, 3);
            foreach (HObject obj in objects)
            {
                HOperatorSet.DispObj(obj, handle);
            }

            MessageBox.Show("exit?");
        }

        public static void SegmentBallRegions(HObject inspectionRegion, HObject extendedGoodBallRegions,
            double ballMinCircularity, double ballPositionOffset, double ballMinAreaPixel, double ballMaxAreaPixel,
            out HObject ballRegions, out Dictionary<HObject, BgaInspectionItem> resultRegion)
        {
            resultRegion = new Dictionary<HObject, BgaInspectionItem>(32);
            HOperatorSet.GenEmptyRegion(out ballRegions);
            HOperatorSet.GenEmptyRegion(out HObject ngUnionRegion);
            HOperatorSet.GenEmptyRegion(out HObject bigBallRegion);
            HOperatorSet.GenEmptyRegion(out HObject smallBallRegion);
            HOperatorSet.GenEmptyRegion(out HObject extraBallRegion);
            HOperatorSet.GenEmptyRegion(out HObject missingBallRegion);
            HOperatorSet.GenEmptyRegion(out HObject emptyBallRegion);
            HOperatorSet.GenEmptyRegion(out HObject ballPitchRegion);
            HOperatorSet.GenEmptyRegion(out HObject bridgingBallRegion);
            HOperatorSet.GenEmptyRegion(out HObject crackBallRegion);

            HOperatorSet.Union1(inspectionRegion, out HObject backupInspectionRegion);
            HOperatorSet.ClosingCircle(inspectionRegion, out inspectionRegion, ballPositionOffset);

            // Ball Pitch, Bridge Ball, Big Ball -> ErrorRegion

            HObject regionIntersection, regionDifference;
            HOperatorSet.Intersection(inspectionRegion, extendedGoodBallRegions, out regionIntersection);
            HOperatorSet.Difference(inspectionRegion, regionIntersection, out regionDifference);

            // regionIntersection과 regionDifference의 영역 중, 서로 붙어 있는 Region만 추출한다
            HOperatorSet.Connection(regionDifference, out regionDifference);
            HOperatorSet.SelectShape(regionDifference, out regionDifference, "area", "and", 2, 99999999);
            HOperatorSet.DilationCircle(regionDifference, out regionDifference, 3);
            VisionOperation.GetSupersetOfSubset(regionIntersection, regionDifference, out HObject bridgeOrPitchOrBigRegion);

            // Bridging Ball Error
            HOperatorSet.SelectShape(bridgeOrPitchOrBigRegion, out bridgingBallRegion, "circularity", "and", 0, ballMinCircularity);
            HOperatorSet.SelectShape(bridgingBallRegion, out bridgingBallRegion, "area", "and", ballMaxAreaPixel * 1.5, ballMaxAreaPixel * 10);
            HOperatorSet.Union1(bridgingBallRegion, out bridgingBallRegion);
            HOperatorSet.Union2(ngUnionRegion, bridgingBallRegion, out ngUnionRegion);
            if (!IsEmpty(bridgingBallRegion))
            {
                resultRegion.Add(bridgingBallRegion, BgaInspectionItem.BallBridging);
            }

            // Ball Pitch, Big Ball -> ErrorRegion
            HOperatorSet.Difference(bridgeOrPitchOrBigRegion, ngUnionRegion, out HObject pitchOrBigBallRegion);

            // Big Ball Error
            HOperatorSet.SelectShape(pitchOrBigBallRegion, out bigBallRegion, "area", "and", ballMaxAreaPixel * 1.2, ballMaxAreaPixel * 5);
            HOperatorSet.SelectShape(bigBallRegion, out bigBallRegion, "circularity", "and", ballMinCircularity, 1);
            HOperatorSet.Union1(bigBallRegion, out bigBallRegion);
            HOperatorSet.Union2(ngUnionRegion, bigBallRegion, out ngUnionRegion);
            if (!IsEmpty(bigBallRegion))
            {
                resultRegion.Add(bigBallRegion, BgaInspectionItem.BallSize);
            }

            // Ball Pitch Error
            HOperatorSet.Difference(pitchOrBigBallRegion, ngUnionRegion, out ballPitchRegion);
            HOperatorSet.Connection(ballPitchRegion, out ballPitchRegion);
            HOperatorSet.SelectShape(ballPitchRegion, out ballPitchRegion, "circularity", "and", ballMinCircularity, 1);
            HOperatorSet.Union1(ballPitchRegion, out ballPitchRegion);
            HOperatorSet.Union2(ngUnionRegion, ballPitchRegion, out ngUnionRegion);
            if (!IsEmpty(ballPitchRegion))
            {
                resultRegion.Add(ballPitchRegion, BgaInspectionItem.BallPitch);
            }

            // Good Ball, Small Ball -> inspectionRegion
            HOperatorSet.Intersection(inspectionRegion, extendedGoodBallRegions, out inspectionRegion);
            HOperatorSet.Difference(inspectionRegion, ngUnionRegion, out inspectionRegion);
            HOperatorSet.OpeningCircle(inspectionRegion, out inspectionRegion, ballPositionOffset);
            HOperatorSet.Connection(inspectionRegion, out inspectionRegion);
            HOperatorSet.SelectShape(inspectionRegion, out inspectionRegion, "circularity", "and", ballMinCircularity, 1);

            // Small Ball Error
            HOperatorSet.OpeningCircle(inspectionRegion, out smallBallRegion, ballPositionOffset);
            HOperatorSet.SelectShape(smallBallRegion, out smallBallRegion, "area", "and", ballMinAreaPixel * 0.1, ballMinAreaPixel * 0.6);
            HOperatorSet.Union1(smallBallRegion, out smallBallRegion);
            HOperatorSet.Union2(ngUnionRegion, smallBallRegion, out ngUnionRegion);
            if (!IsEmpty(smallBallRegion))
            {
                resultRegion.Add(smallBallRegion, BgaInspectionItem.BallSize);
            }

            // OK Balls
            HOperatorSet.Difference(inspectionRegion, ngUnionRegion, out inspectionRegion);
            HOperatorSet.OpeningCircle(inspectionRegion, out ballRegions, ballPositionOffset);
            HOperatorSet.FillUp(ballRegions, out ballRegions);

            //// Empty Ball
            //HOperatorSet.Difference(extendedGoodBallRegions, backupInspectionRegion, out emptyBallRegion);
            //HOperatorSet.SelectShape(emptyBallRegion, out emptyBallRegion, "circularity", "and", 0.99, 1);
            //HOperatorSet.Union1(emptyBallRegion, out emptyBallRegion);
            //HOperatorSet.Union2(ngUnionRegion, emptyBallRegion, out ngUnionRegion);
            //if (!IsEmpty(emptyBallRegion))
            //{
            //    resultRegion.Add(emptyBallRegion, BgaInspectionItem.MissingBall);
            //}

            //// Crack Ball
            //HOperatorSet.Difference(backupInspectionRegion, ngUnionRegion, out crackBallRegion);
            //HOperatorSet.Difference(crackBallRegion, ballRegions, out crackBallRegion);

            //HOperatorSet.OpeningCircle(crackBallRegion, out crackBallRegion, ballPositionOffset);
            //VisionOperation.GetSupersetOfSubset(extendedGoodBallRegions, crackBallRegion, out crackBallRegion);
            //if (!IsEmpty(crackBallRegion))
            //{
            //    resultRegion.Add(crackBallRegion, BgaInspectionItem.CrackBall);
            //}

            // End!
            HOperatorSet.FillUp(ballRegions, out ballRegions);
            HOperatorSet.FillUp(ngUnionRegion, out ngUnionRegion);
            HOperatorSet.Difference(ballRegions, ngUnionRegion, out ballRegions);
        }


        public static void FindCircles(HObject image, Threshold threshold, int minArea, int maxArea, double minCircularity, out HObject circleRegions, out List<Circle> circles)
        {
            try
            {
                VisionOperation.Threshold(image, threshold, out HObject region);
                HOperatorSet.Connection(region, out region);
                HOperatorSet.SelectShape(region, out region, "circularity", "and", minCircularity / 100, 1);
                HOperatorSet.SelectShape(region, out region, "area", "and", minArea, maxArea);

                HOperatorSet.GenContourRegionXld(region, out HObject contours, "border");
                region.Dispose();
                HOperatorSet.FitCircleContourXld(contours, "ahuber", -1, 0, 0, 3, 2, out HTuple row, out HTuple column, out HTuple radius, out _, out _, out _);
                contours.Dispose();

                HOperatorSet.GenCircle(out circleRegions, row, column, radius);

                circles = new List<Circle>(circleRegions.CountObj());
                for (int i = 0; i < row.Length; i++)
                {
                    circles.Add(new Circle(column[i].D, row[i].D, radius[i].D));
                }
            }
            catch
            {
                HOperatorSet.GenEmptyRegion(out circleRegions);
                circles = new List<Circle>(0);
            }
        }

        public static void FindRectangles(HObject image, Threshold threshold, int min, int max, double rectangularity, out HObject rectangleRegions, out List<Rect2> rectangles)
        {
            try
            {
                Threshold(image, threshold, out HObject region);
                HOperatorSet.Connection(region, out region);
                HOperatorSet.SelectShape(region, out region, "rectangularity", "and", rectangularity / 100, 1);
                HOperatorSet.SelectShape(region, out region, "area", "and", min, max);

                HOperatorSet.GenContourRegionXld(region, out HObject contours, "border");
                HOperatorSet.FitRectangle2ContourXld(contours, "regression", -1, 0, 0, 3, 2, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2, out _);
                region.Dispose();

                HOperatorSet.GenRectangle2(out rectangleRegions, row, column, phi, length1, length2);

                rectangles = new List<Rect2>(rectangleRegions.CountObj());
                for (int i = 0; i < row.Length; i++)
                {
                    rectangles.Add(new Rect2(row[i].D, column[i].D, phi[i].D, length1[i].D, length2[i].D));
                }
            }
            catch
            {
                HOperatorSet.GenEmptyRegion(out rectangleRegions);
                rectangles = new List<Rect2>(0);
            }
        }

        public static void FindRectangle2(HObject image, Point midPoint, Threshold threshold, out HObject rectangle2, out Pose pose)
        {
            try
            {
                FindMostMidRectangle(image, midPoint, threshold, out HObject region, out _);

                HOperatorSet.FillUp(region, out region);
                HOperatorSet.GenContourRegionXld(region, out HObject contours, "border");
                HOperatorSet.FitRectangle2ContourXld(contours, "regression", -1, 0, 0, 3, 2, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2, out _);
                region.Dispose();

                HOperatorSet.GenRectangle2(out rectangle2, row, column, phi, length1, length2);

                HTuple angle = phi.TupleDeg();

                if (angle > 45)
                {
                    angle -= 90;
                }

                if (angle < -45)
                {
                    angle += 90;
                }

                pose = new Pose(column.D, row.D, angle.D);
            }
            catch
            {
                HOperatorSet.GenEmptyRegion(out rectangle2);
                pose = new Pose(-1, -1, -1);
            }
        }

        public static void FindBallRoiAuto(HObject image, out Roi roi)
        {
            roi = null;
            try
            {
                HTuple minThreshold, tempSizeOffset = 0;
                for (minThreshold = 254; (int)minThreshold >= 0; minThreshold = (int)minThreshold - 1)
                {
                    HObject region;
                    HOperatorSet.Threshold(image, out region, minThreshold, 255);
                    HOperatorSet.Connection(region, out region);
                    HOperatorSet.SelectShape(region, out region, "circularity", "and", 0.6, 1);
                    HTuple areas;
                    HOperatorSet.RegionFeatures(region, "area", out areas);

                    if ((int)new HTuple(new HTuple(areas.TupleLength()).TupleEqual(0)) != 0)
                    {
                        continue;
                    }

                    HTuple mean;
                    HOperatorSet.TupleMean(areas, out mean);
                    HTuple sizeOffset = mean * 0.4;
                    HOperatorSet.SelectShape(region, out region, "area", "and", mean - sizeOffset, mean + sizeOffset);

                    if ((int)(new HTuple(sizeOffset.TupleLess(tempSizeOffset - 5))) != 0)
                    {
                        HTuple balls;
                        HOperatorSet.RegionFeatures(region, "column1", out balls);
                        HTuple singleBallPitch = (balls.TupleSelect(1) - balls.TupleSelect(0)) * 4;

                        HOperatorSet.TupleAbs(singleBallPitch, out singleBallPitch);
                        HOperatorSet.Union1(region, out region);
                        HOperatorSet.ClosingRectangle1(region, out region, singleBallPitch, singleBallPitch);
                        HOperatorSet.DilationRectangle1(region, out region, 11, 11);
                        HOperatorSet.Connection(region, out region);

                        HTuple row1, col1, row2, col2;
                        HOperatorSet.RegionFeatures(region, "row1", out row1);
                        HOperatorSet.RegionFeatures(region, "column1", out col1);
                        HOperatorSet.RegionFeatures(region, "row2", out row2);
                        HOperatorSet.RegionFeatures(region, "column2", out col2);

                        roi = new Roi("ROI 1", row1.D, col1.D, row2.D, col2.D);
                        break;
                    }

                    tempSizeOffset = new HTuple(sizeOffset);
                }

                if (roi == null)
                {
                    throw new VisionNotFoundException();
                }
            }
            catch
            {
                throw new VisionNotFoundException();
            }
        }

        public static void GetHObjectSize(HObject region, out double width, out double height, out double area)
        {
            HOperatorSet.RegionFeatures(region, "width", out HTuple tupleWidth);
            HOperatorSet.RegionFeatures(region, "height", out HTuple tupleHeight);
            HOperatorSet.RegionFeatures(region, "area", out HTuple tupleArea);
            width = tupleWidth.D;
            height = tupleHeight.D;
            area = tupleArea.D;
        }

        public static void FindRect(HObject image, Roi roi, Threshold threshold, out Rect rect)
        {
            ReduceDomain(image, roi, out HObject reducedImage);
            Threshold(reducedImage, threshold, out HObject region);
            reducedImage.Dispose();

            HOperatorSet.Connection(region, out region);
            HOperatorSet.SelectShapeStd(region, out region, "max_area", 70);
            Region2Roi(region, out Roi firstPinRoi);
            region.Dispose();

            rect = new Rect(firstPinRoi);
        }

        public static void FindRects(HObject image, List<Roi> rois, Threshold threshold, out List<Rect> rects)
        {
            ReduceDomain(image, rois, out HObject reducedImage);
            Threshold(reducedImage, threshold, out HObject region);
            HOperatorSet.OpeningCircle(region, out region, 5);
            reducedImage.Dispose();

            VisionOperation.Region2Rois(region, out List<Roi> patterns);
            VisionOperation.Rois2UnionRegion(patterns, out region);

            HOperatorSet.Union1(region, out region);
            HOperatorSet.Connection(region, out region);
            Region2Rois(region, out patterns);
            Rois2Rects(patterns, out rects);
            region.Dispose();
        }

        public static void Rect2Region(Rect rect, out HObject region)
        {
            HOperatorSet.GenRectangle1(out region, rect.Row1, rect.Col1, rect.Row2, rect.Col2);
        }

        public static void Rects2Region(List<Rect> rects, out HObject region)
        {
            HOperatorSet.GenEmptyObj(out region);
            foreach (Rect rect in rects)
            {
                HOperatorSet.GenRectangle1(out HObject rectangle1, rect.Row1, rect.Col1, rect.Row2, rect.Col2);
                HOperatorSet.ConcatObj(region, rectangle1, out region);
                rectangle1.Dispose();
            }
        }

        public static void Rois2Rects(List<Roi> rois, out List<Rect> rects)
        {
            rects = rois.Select(roi => new Rect(roi)).ToList();
        }

        public static void Rects2Rois(List<Rect> rects, out List<Roi> rois)
        {
            rois = rects.Select(rect => new Roi(rect)).ToList();
        }

        public static void ReduceDomain(HObject image, HObject region, out HObject reducedImage)
        {
            HOperatorSet.Union1(region, out HObject regionUnion);
            HOperatorSet.ReduceDomain(image, regionUnion, out reducedImage);
            regionUnion.Dispose();
        }

        public static void ReduceDomain(HObject image, List<HObject> regions, out HObject reducedImage)
        {
            HOperatorSet.GenEmptyRegion(out HObject regionUnion);
            foreach (HObject region in regions)
            {
                HOperatorSet.Union2(regionUnion, region, out regionUnion);
                region.Dispose();
            }

            HOperatorSet.ReduceDomain(image, regionUnion, out reducedImage);
            regionUnion.Dispose();
        }

        public static void ReduceDomain(HObject image, Roi roi, out HObject reducedImage)
        {
            Roi2Region(roi, out HObject region);
            ReduceDomain(image, region, out reducedImage);
            region.Dispose();
        }

        public static void ReduceDomain(HObject image, List<Roi> rois, out HObject reducedImage)
        {
            VisionOperation.Rois2UnionRegion(rois, out HObject region);
            VisionOperation.ReduceDomain(image, region, out reducedImage);
            region.Dispose();
        }

        public static void ReduceDomainComplement(HObject image, HObject region, out HObject reducedImage)
        {
            HOperatorSet.Union1(region, out HObject regionUnion);
            HOperatorSet.Complement(regionUnion, out HObject regionComplement);
            if (IsEmpty(regionComplement))
            {
                reducedImage = image;
                return;
            }

            regionUnion.Dispose();
            HOperatorSet.ReduceDomain(image, regionComplement, out reducedImage);
            regionComplement.Dispose();
        }

        public static void ReduceDomainComplement(HObject image, Roi roi, out HObject reducedImage)
        {
            Roi2Region(roi, out HObject region);
            ReduceDomainComplement(image, region, out reducedImage);
            region.Dispose();
        }

        public static void ReduceDomainComplement(HObject image, List<Roi> rois, out HObject reducedImage)
        {
            VisionOperation.Rois2UnionRegion(rois, out HObject regions);
            VisionOperation.ReduceDomainComplement(image, regions, out reducedImage);
            regions.Dispose();
        }

        public static void Circles2Regions(List<Circle> circles, out HObject regions, double dilation = 0)
        {
            HTuple rows = new HTuple(circles.Select(b => b.Y).ToArray());
            HTuple columns = new HTuple(circles.Select(b => b.X).ToArray());
            HTuple radii = new HTuple(circles.Select(b => b.Radius).ToArray());
            HOperatorSet.GenCircle(out regions, rows, columns, radii);
        }

        public static void Difference(HObject region, HObject subRegion, out HObject diffRegion)
        {
            HOperatorSet.Union1(subRegion, out HObject subRegionUnion);
            HOperatorSet.Difference(region, subRegionUnion, out diffRegion);
            subRegionUnion.Dispose();
        }

        public static void GetCrossOfRegionCenter(HObject region, out HObject crossContour)
        {
            HOperatorSet.SmallestRectangle2(region, out HTuple row, out HTuple column, out HTuple phi, out _, out _);
            HOperatorSet.GenCrossContourXld(out crossContour, row, column, 50, phi);
        }

        public static void GetCenterPointsOfRegions(HObject regions, out List<Point> points)
        {
            points = new List<Point>(regions.CountObj());

            for (int i = 1; i <= regions.CountObj(); i++)
            {
                HOperatorSet.SelectObj(regions, out HObject selectedRegion, i);

                HOperatorSet.RegionFeatures(selectedRegion, "row", out HTuple row);
                HOperatorSet.RegionFeatures(selectedRegion, "column", out HTuple column);
                selectedRegion.Dispose();

                points.Add(new Point(row.D, column.D));
            }
        }

        public static void GetImageMidPoint(HObject image, out Point midPoint)
        {
            HOperatorSet.RegionFeatures(image, "width", out HTuple imageWidth);
            HOperatorSet.RegionFeatures(image, "height", out HTuple imageHeight);

            midPoint = new Point((int)imageHeight.D / 2, (int)imageWidth.D / 2);
        }

        public static double GetDistanceOfTwoPoint(Point p1, Point p2)
        {
            double dRow = p1.Row - p2.Row;
            double dColumn = p1.Col - p2.Col;
            return Math.Sqrt(dRow * dRow + dColumn * dColumn);
        }

        public static void FindCenterPose(List<Pose> poses, Point imageCenterPoint, out Pose centerPose)
        {
            centerPose = poses.First();
            double minDistance = double.MaxValue;

            foreach (Pose pose in poses)
            {
                double distance = GetDistanceOfTwoPoint(new Point(pose.Y, pose.X), imageCenterPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    centerPose = pose;
                }
            }
        }

        public static void FindCenterPoint(List<Point> points, Point imageCenterPoint, out Point centerPoint)
        {
            centerPoint = points.First();
            double minDistance = double.MaxValue;

            foreach (Point point in points)
            {
                double distance = GetDistanceOfTwoPoint(point, imageCenterPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    centerPoint = point;
                }
            }
        }

        public static void GetMostMidRegion(HObject regions, Point imageCenterPoint, out HObject mostMidRegion)
        {
            GetCenterPointsOfRegions(regions, out List<Point> points);
            FindCenterPoint(points, imageCenterPoint, out Point mostMiddlePoint);
            HOperatorSet.SelectRegionPoint(regions, out mostMidRegion, new HTuple(mostMiddlePoint.Row), new HTuple(mostMiddlePoint.Col));
        }

        public static void FindMostMidRectangle(HObject image, Point midPoint, Threshold threshold, out HObject rectangleRegion, out Pose pose)
        {
            try
            {
                Threshold(image, threshold, out HObject region);
                HOperatorSet.Connection(region, out region);

                HOperatorSet.SelectShapeStd(region, out region, "rectangle2", 70);
                HOperatorSet.SelectShape(region, out region, "area", "and", 100, 9999999999);

                GetMostMidRegion(region, midPoint, out rectangleRegion);
                region.Dispose();

                HOperatorSet.FillUp(rectangleRegion, out rectangleRegion);
                HOperatorSet.GenContourRegionXld(rectangleRegion, out HObject contours, "border");
                HOperatorSet.FitRectangle2ContourXld(contours, "regression", -1, 0, 0, 3, 2, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2, out _);
                HOperatorSet.GenRectangle2(out rectangleRegion, row, column, phi, length1, length2);

                HTuple angle = phi.TupleDeg();

                if (angle > 45)
                {
                    angle -= 90;
                }

                if (angle < -45)
                {
                    angle += 90;
                }

                pose = new Pose(column.D, row.D, angle.D);
            }
            catch
            {
                throw new VisionNotFoundException();
            }
        }

        public static void FindMostMidCircle(HObject image, Threshold threshold, out HObject circleRegion, out Circle circle)
        {
            try
            {
                HOperatorSet.FastThreshold(image, out HObject region, threshold.MinGray, threshold.MaxGray, 20);
                HOperatorSet.Connection(region, out region);
                HOperatorSet.SelectShape(region, out region, "circularity", "and", 0.9, 1);
                HOperatorSet.SelectShape(region, out region, "area", "and", 150, 99999);

                GetImageMidPoint(image, out Point midPoint);
                GetMostMidRegion(region, midPoint, out circleRegion);
                region.Dispose();

                HOperatorSet.GenContourRegionXld(circleRegion, out HObject contours, "border");
                HOperatorSet.FitCircleContourXld(contours, "ahuber", -1, 0, 0, 3, 2, out HTuple row, out HTuple column, out HTuple radius, out _, out _, out _);

                HOperatorSet.GenCircle(out circleRegion, row, column, radius);
                circle = new Circle(column, row, radius);
            }
            catch
            {
                throw new VisionNotFoundException();
            }
        }

        public static void GetTopLeftPosition(HObject region, out Point point)
        {
            try
            {
                HOperatorSet.RegionFeatures(region, "row1", out HTuple row1);
                HOperatorSet.RegionFeatures(region, "column1", out HTuple col1);
                point = new Point(row1[0].D, col1[0].D);
            }
            catch
            {
                throw new GVisionException();
            }
        }

        public static void GetAreaRatio(HObject region, int originalArea, out Ratio ratio)
        {
            try
            {
                HOperatorSet.Union1(region, out HObject unionRegion);
                HOperatorSet.RegionFeatures(unionRegion, "area", out HTuple area);
                ratio = new Ratio(area.D * 100 / originalArea);
            }
            catch
            {
                ratio = new Ratio(0);
            }
        }

        public static void GetRegionPerimeter(HObject region, out Length perimeter)
        {
            try
            {
                HOperatorSet.FillUp(region, out HObject inspectionRegion);
                HOperatorSet.RegionFeatures(inspectionRegion, "contlength", out HTuple value);
                inspectionRegion.Dispose();

                perimeter = new Length(value.D);
            }
            catch
            {
                perimeter = new Length(0);
            }
        }

        public static void Distance(HObject regions, out List<Length> distances)
        {
            distances = new List<Length>(32);
            try
            {
                HOperatorSet.CountObj(regions, out HTuple count);

                if (count <= 1)
                {
                    distances.Add(new Length(0));
                }

                for (int i = 1; i < count; i++)
                {
                    HOperatorSet.SelectObj(regions, out HObject region1, i);
                    HOperatorSet.SelectObj(regions, out HObject region2, i + 1);
                    HOperatorSet.DistanceRrMinDil(region1, region2, out HTuple minDistance);

                    distances.Add(new Length(minDistance.D));
                }
            }
            catch
            {
                // ignore
            }
        }

        public static void Distance(HObject targetRegion, Point start, Point end, out Length distance)
        {
            try
            {
                HOperatorSet.DistanceLr(targetRegion, start.Row, start.Col, end.Row, end.Col, out HTuple distanceMin, out _);
                distance = new Length(distanceMin.D);
            }
            catch
            {
                distance = new Length(0);
            }
        }

        public static void SmallestRectangle2(HObject region, out HObject rectRegion)
        {
            HOperatorSet.SmallestRectangle2(region, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
            HOperatorSet.GenRectangle2(out rectRegion, row, column, phi, length1, length2);
        }

        public static void Stretch(HObject region, int dilation, out HObject stretchedRegion)
        {
            HOperatorSet.GenEmptyRegion(out stretchedRegion);
            HOperatorSet.RegionFeatures(region, "phi", out HTuple phis);
            for (int i = 0; i < phis.Length; i++)
            {
                HOperatorSet.GenRectangle2(out HObject rectangle2, 100, 100, phis[i], dilation, 0);
                HOperatorSet.Dilation1(region[i + 1], rectangle2, out HObject regionDilation, 1);
                rectangle2.Dispose();
                HOperatorSet.ConcatObj(stretchedRegion, regionDilation, out stretchedRegion);
                regionDilation.Dispose();
            }
        }

        public static void GetAdjacentRegions(HObject sourceRegion, HObject regions, out HObject neighborRegion)
        {
            HOperatorSet.FindNeighbors(sourceRegion, regions, 2, out _, out HTuple indices);
            HOperatorSet.SelectObj(regions, out neighborRegion, indices);
        }

        public static void GetLineFromPackagePoints(List<Point> packagePoints, EDirection direction, out Point start, out Point end)
        {
            int index = (int)direction;
            start = packagePoints[index];
            end = packagePoints[index + 1];
        }

        public static void Distance(Point start, Point end, HObject targetRegion, out Length distance, out HObject footOfPerpendicular)
        {
            HOperatorSet.GenEmptyRegion(out footOfPerpendicular);

            HOperatorSet.GenRegionLine(out HObject lineRegion, start.Row, start.Col, end.Row, end.Col);

            try
            {
                HOperatorSet.DistanceRrMin(lineRegion, targetRegion, out HTuple minDistance, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                HOperatorSet.GenRegionLine(out footOfPerpendicular, row1, col1, row2, col2);
                distance = new Length(minDistance.D);
            }
            catch
            {
                throw new VisionNotFoundException();
            }
        }

        public static void GenGrayHistogram(HObject image, out HObject histogram)
        {
            HOperatorSet.GrayHisto(image, image, out HTuple absoluteHistogram, out _);
            HOperatorSet.GenRegionHisto(out histogram, absoluteHistogram, 10, 10, 3);
            HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
            HOperatorSet.ZoomRegion(histogram, out histogram, width / 200, height / 200);
        }

        public static void GenLineProfile(HObject image, Point start, Point end, out HObject lineProfile, int samplingPoints = 100)
        {
            try
            {
                double deltaRow = (end.Row - start.Row) / (samplingPoints - 1);
                double deltaCol = (end.Col - start.Col) / (samplingPoints - 1);
                HTuple rows = HTuple.TupleGenSequence(start.Row, end.Row, deltaRow);
                HTuple cols = HTuple.TupleGenSequence(start.Col, end.Col, deltaCol);

                HOperatorSet.GenContourPolygonXld(out HObject lineContour, rows, cols);
                HOperatorSet.GetGrayvalContourXld(image, lineContour, "bilinear", out HTuple grayValues);

                HOperatorSet.GenRegionHisto(out lineProfile, grayValues, 10, 10, 3);
                HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
                HOperatorSet.ZoomRegion(lineProfile, out lineProfile, width / 200, height / 200);
            }
            catch
            {
                HOperatorSet.GenEmptyRegion(out lineProfile);
            }
        }

        public static void GetMaxAreaRegion(HObject regions, out HObject maxRegion)
        {
            HOperatorSet.AreaCenter(regions, out HTuple area, out _, out _);
            HOperatorSet.SelectShape(regions, out maxRegion, "area", "and", area.TupleMax(), 9999999999);
        }

        public static void GenMatchingModel(HObject image, Roi modelRoi, out HTuple matchResultNums, out HObject matchingRegions)
        {
            // 초기화
            HOperatorSet.GenEmptyRegion(out matchingRegions);
            HOperatorSet.GenEmptyObj(out HObject allContours);
            // 영역 설정
            Roi2Region(modelRoi, out HObject modelRegion);
            ReduceDomain(image, modelRegion, out HObject modelImage);
            modelRegion.Dispose();
            //모델 생성
            HOperatorSet.CreateGenericShapeModel(out HTuple modelID);
            HOperatorSet.SetGenericShapeModelParam(modelID, "metric", "use_polarity");
            HOperatorSet.TrainGenericShapeModel(modelImage, modelID);
            modelImage.Dispose();
            // set modelParam
            HOperatorSet.SetGenericShapeModelParam(modelID, "min_score", 0.9);
            HOperatorSet.SetGenericShapeModelParam(modelID, "border_shape_models", "false");
            HOperatorSet.SetGenericShapeModelParam(modelID, "greediness", 0.6);
            // 모델 찾기
            HOperatorSet.FindGenericShapeModel(image, modelID, out HTuple matchResultID, out matchResultNums);
            HOperatorSet.GetGenericShapeModelResultObject(out HObject matchContour, matchResultID, "all", "contours");
            HOperatorSet.ConcatObj(allContours, matchContour, out allContours);
            HOperatorSet.GenRegionContourXld(allContours, out matchingRegions, "filled");
            HOperatorSet.Union1(matchingRegions, out HObject regionUnion);
            HOperatorSet.Connection(regionUnion, out matchingRegions);
            // 모델 ID 메모리 해제
            HOperatorSet.ClearShapeModel(modelID);
            allContours.Dispose();
            regionUnion.Dispose();
            image.Dispose();
            matchContour.Dispose();
        }

        public static void FlipImageHorizontally(HObject image, out HObject flippedImage)
        {
            HOperatorSet.MirrorImage(image, out flippedImage, "column");
        }

        public static void FlipImageVertically(HObject image, out HObject flippedImage)
        {
            HOperatorSet.MirrorImage(image, out flippedImage, "row");
        }

        public static bool IsPointInRegion(Point point, HObject maxRegion)
        {
            HOperatorSet.TestRegionPoint(maxRegion, point.Row, point.Col, out HTuple isInside);
            return isInside.D > 0;
        }

        public static double Radian2DegreeForOffset(double phi)
        {
            HOperatorSet.TupleDeg(phi, out HTuple degree);

            if (degree > 45)
            {
                degree -= 90;
            }

            if (degree < -45)
            {
                degree += 90;
            }

            return degree.D;
        }

        public static int GetCountOf(HObject hobject)
        {
            return IsEmpty(hobject) ? 0 : hobject.CountObj();
        }

        public static void ComputeDelaunayEdges(List<Point> points, out List<Line> edges)
        {
            IPoint[] array = points.Select(p => (IPoint)new DelaunatorSharp.Point(p.Col, p.Row)).ToArray();
            Delaunator delaunator = new Delaunator(array);

            List<Line> tmpEdges = new List<Line>();
            delaunator.ForEachTriangleEdge(edge =>
            {
                Point start = new Point(edge.P.Y, edge.P.X);
                Point end = new Point(edge.Q.Y, edge.Q.X);
                tmpEdges.Add(new Line(start, end));
            });

            edges = tmpEdges;
        }

        public static void CalculateEuclideanDistance(Point p1, Point p2, out Length distance)
        {
            double dx = p1.Col - p2.Col;
            double dy = p1.Row - p2.Row;
            distance = new Length(Math.Sqrt(dx * dx + dy * dy));
        }

        public static bool HasIntermediatePoint(Line line, KdTree<double, Point> kdTree, double tolerance)
        {
            Point midPoint = new Point(
                row: (line.Start.Row + line.End.Row) / 2.0,
                col: (line.Start.Col + line.End.Col) / 2.0
            );

            double dx = line.End.Col - line.Start.Col;
            double dy = line.End.Row - line.Start.Row;
            double length = Math.Sqrt(dx * dx + dy * dy);
            double searchRadius = (length / 2.0) + tolerance;

            KdTreeNode<double, Point>[] nodes = kdTree.RadialSearch(new[] { midPoint.Row, midPoint.Col }, searchRadius);
            foreach (KdTreeNode<double, Point> node in nodes)
            {
                Point candidatePoint = node.Value;
                if (VisionOperation.IsApproximatelyEqual(candidatePoint, line.Start) || VisionOperation.IsApproximatelyEqual(candidatePoint, line.End))
                {
                    continue;
                }

                /*
                 * 직선(line)에 점(candidatePoint)를 projection시킨 점(projectedPoint)를 구합니다.
                 * 그 전에, projectedPoint가 선분 내부에 위치하는지 판단합니다.
                 * if 0 ≤ t ≤ 1, then 선분 사이에 점이 존재한다.
                 * else, 선분 밖에 점이 존재한다.
                 */
                double t = ((candidatePoint.Row - line.Start.Row) * dy + (candidatePoint.Col - line.Start.Col) * dx) / (length * length);
                if (t < 0 || t > 1)
                {
                    continue;
                }

                Point projectedPoint = new Point(line.Start.Row + t * dy, line.Start.Col + t * dx);
                VisionOperation.CalculateEuclideanDistance(candidatePoint, projectedPoint, out Length distance);
                if (distance.Value < tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        public static double CalculateLineAngle(Line line)
        {
            double dx = line.Start.Col - line.End.Col;
            double dy = line.Start.Row - line.End.Row;
            return Math.Atan2(dy, dx) * (180 / Math.PI);
        }

        public static bool IsApproximatelyEqual(Point a, Point b, double tol = 1e-3)
        {
            return Math.Abs(a.Row - b.Row) < tol && Math.Abs(a.Col - b.Col) < tol;
        }

        public static void ApplyMarginToRoi(Roi sourceRoi, int horizontalMarginPercent, int verticalMarginPercent, out Roi resultRoi)
        {
            double width = sourceRoi.Col2 - sourceRoi.Col1;
            double height = sourceRoi.Row2 - sourceRoi.Row1;

            double deltaX = width * horizontalMarginPercent / 100.0 / 2.0;
            double deltaY = height * verticalMarginPercent / 100.0 / 2.0;

            double newRow1 = sourceRoi.Row1 - deltaY;
            double newCol1 = sourceRoi.Col1 - deltaX;
            double newRow2 = sourceRoi.Row2 + deltaY;
            double newCol2 = sourceRoi.Col2 + deltaX;

            resultRoi = new Roi(sourceRoi.Name, newRow1, newCol1, newRow2, newCol2);
        }

        public static void ApplyMarginToRois(List<Roi> rois, int horizontalMarginPercent, int verticalMarginPercent, out List<Roi> marginRois)
        {
            marginRois = new List<Roi>(rois.Count);
            foreach (Roi roi in rois)
            {
                VisionOperation.ApplyMarginToRoi(roi, horizontalMarginPercent, verticalMarginPercent, out Roi packageRoi);
                marginRois.Add(packageRoi);
            }
        }

        public static void PartitionRoi(Roi roi, int nRow, int nCol, out List<Roi> rois)
        {
            VisionOperation.PartitionRectangle(roi, nRow, nCol, out HObject partitionRegion);
            HOperatorSet.SortRegion(partitionRegion, out partitionRegion, "character", "true", "row");
            VisionOperation.Region2Rois(partitionRegion, out rois);
            partitionRegion.Dispose();
        }

        public static void ElementWiseQuadraticMean(HTuple tuple1, HTuple tuple2, out HTuple quadraticMean)
        {
            HOperatorSet.TuplePow(tuple1, 2.0, out HTuple tuple1Square);
            HOperatorSet.TuplePow(tuple2, 2.0, out HTuple tuple2Square);
            HOperatorSet.TupleAdd(tuple1Square, tuple2Square, out HTuple sumOfSquares);
            HOperatorSet.TupleDiv(sumOfSquares, 2.0, out HTuple meanOfSquares);
            HOperatorSet.TupleSqrt(meanOfSquares, out quadraticMean);
        }

        public static void CalculateFootOfPerpendicular(Point point, Point lineStart, Point lineEnd, out Point footOfPerpendicular)
        {
            HOperatorSet.ProjectionPl(point.Row, point.Col, lineStart.Row, lineStart.Col, lineEnd.Row, lineEnd.Col, out HTuple row, out HTuple col);
            footOfPerpendicular = new Point(row.D, col.D);
        }

        public static void SelectRegionsByPoints(HObject sourceRegion, HTuple rows, HTuple cols, out HObject selectedRegion)
        {
            HOperatorSet.SelectRegionPoint(sourceRegion, out selectedRegion, rows, cols);
        }
    }
}



