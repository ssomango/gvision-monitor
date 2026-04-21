using System;
using System.Collections.Generic;
using System.Linq;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Visions;
using GVisionWpf.Repositories;
using GVisionWpf.Types;
using HalconDotNet;
using KdTree.Math;
using KdTree;
using Point = GVisionWpf.Models.Visions.Point;
using Size = GVisionWpf.Models.Visions.Size;
using GVisionWpf.UIs.ViewModels.TreeView;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Drawing;

namespace GVisionWpf.Visions.Engines
{
    public class BgaEngine : VisionEngine
    {
        public static Result<Size> InspectPackageSize(HObject packageRegion, out FixedText text)
        {
            return VisionEngine.InspectPackageSize(packageRegion, DeviceRecipeRepository.Instance.GetRecipe().PackageSize, GlobalSetting.Instance.Inspection.Tolerance.BgaPackageSize, ECamera.PRS, out text);
        }

        public static Result<SawOffset> InspectSawOffset(List<SawOffsetItem> teachingSawOffsetItems, List<Point> packagePoints, HObject firstPinRegion, HObject patternRegion, HObject ballRegion, out HObject footOfPerpendicular)
        {
            Dictionary<ESawOffsetStandardObject, HObject> targetDictionary = new Dictionary<ESawOffsetStandardObject, HObject>
            {
                [ESawOffsetStandardObject.FirstPin] = firstPinRegion,
                [ESawOffsetStandardObject.Pattern] = patternRegion,
                [ESawOffsetStandardObject.Ball] = ballRegion
            };

            return VisionEngine.InspectSawOffset(teachingSawOffsetItems, packagePoints, targetDictionary, GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetX, GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetY, ECamera.PRS, out footOfPerpendicular);
        }

        public static Result<CornerDegree> InspectCornerDegree(List<Point> points, out HObject cornerPoint, out List<FloatingText> texts)
        {
            double tolerance = GlobalSetting.Instance.Inspection.Tolerance.BgaCornerDegree;
            return VisionEngine.InspectCornerDegree(points, tolerance, out cornerPoint, out texts);
        }

        public static void FindBalls(HObject image, List<Roi> ballRois, Threshold threshold, double minArea, double maxArea, int minCircularity, Double BallPositionOffset,
            out HObject ballRegions, out Dictionary<string, List<Circle>> ballsByRoi)
        {
            ballsByRoi = new Dictionary<string, List<Circle>>();
            HOperatorSet.GenEmptyObj(out ballRegions);

            foreach (Roi roi in ballRois)
            {
                VisionOperation.ReduceDomain(image, roi, out HObject ballRoiImage);
                VisionOperation.Threshold(ballRoiImage, threshold, out HObject thresholdRegion);
                ballRoiImage.Dispose();
                HOperatorSet.Connection(thresholdRegion, out HObject connectedRegions);
                thresholdRegion.Dispose();
                HOperatorSet.SelectShape(connectedRegions, out HObject circleRegions, "circularity", "and", (double)minCircularity / 100, 1);
                connectedRegions.Dispose();
                HOperatorSet.SelectShape(circleRegions, out HObject selectedRegion, "area", "and", minArea , maxArea );
                circleRegions.Dispose();
                HOperatorSet.DilationCircle(selectedRegion, out HObject regionDilation, BallPositionOffset);
                selectedRegion.Dispose();
                HOperatorSet.FillUp(regionDilation, out HObject regionFillUp);
                regionDilation.Dispose();

                HOperatorSet.AreaCenterGray(regionFillUp, image, out HTuple volume, out HTuple row, out HTuple column);
                HOperatorSet.EllipticAxisGray(regionFillUp, image, out HTuple majorRadius, out HTuple minorRadius, out HTuple phi);
                regionFillUp.Dispose();
                VisionOperation.ElementWiseQuadraticMean(majorRadius, minorRadius, out HTuple radius);

                List<Circle> ballCircles = new List<Circle>(radius.Length);
                for (int i = 0; i < radius.Length; i++)
                {
                    double x = column[i].D;
                    double y = row[i].D;
                    double r = radius[i].D;
                    ballCircles.Add(new Circle(x, y, r));
                }
                ballsByRoi[roi.Name] = ballCircles;

                VisionOperation.Circles2Regions(ballCircles, out HObject tmpBallRegions);

                // 누수 방지를 위해 ballRegions 복사본 유지
                HObject oldBallRegions = ballRegions;
                HOperatorSet.ConcatObj(oldBallRegions, tmpBallRegions, out ballRegions);
                oldBallRegions.Dispose(); // 이전 객체 해제
                tmpBallRegions.Dispose();
            }
        }

        public static Result<int> InspectMissingBall(HObject image, HObject ballRegions, List<Roi> ballRois, List<Circle> templateBalls, Threshold threshold, double minArea, double maxArea, out HObject missingBallRegion, out HObject noMissingBallRegion)
        {
            
            // reducedImage = 티칭볼 영역 생성
            VisionOperation.ReduceDomain(image, ballRois, out HObject ballRoiImage);
            VisionOperation.Circles2Regions(templateBalls, out HObject templateBallRegions);
            VisionOperation.ReduceDomain(ballRoiImage, templateBallRegions, out HObject reducedImage);
            ballRoiImage.Dispose();
            templateBallRegions.Dispose();
            

            //후보생성 = 면적 기반으로
            VisionOperation.Threshold(reducedImage, threshold, out HObject thresholdRegion);
            reducedImage.Dispose();
            HOperatorSet.Connection(thresholdRegion, out HObject connectedRegions);
            thresholdRegion.Dispose();
            HOperatorSet.SelectShape(connectedRegions, out HObject missingCandidateBallRegion, "area", "and", minArea, maxArea);
            connectedRegions.Dispose();

            // 기존 볼들과 비교하여 누락된 영역 찾기
            VisionOperation.GetSupersetOfSubset(ballRegions, missingCandidateBallRegion, out noMissingBallRegion);
            HOperatorSet.Union1(missingCandidateBallRegion, out HObject missingCandidateBallRegionUnion);
            missingCandidateBallRegion.Dispose();
            HOperatorSet.Difference(missingCandidateBallRegionUnion, noMissingBallRegion, out missingBallRegion);
            missingCandidateBallRegionUnion.Dispose();
            noMissingBallRegion.Dispose();

            int count = VisionOperation.GetCountOf(missingBallRegion);
            EResultType type = count == 0 ? EResultType.Good : EResultType.MissingBall;
            return new Result<int>(type, count);

        }

        public static Result<int> InspectExtraBall(HObject ballRegions, List<Circle> teachingTemplateBalls, out HObject extraBallRegion)
        {
            VisionOperation.Circles2Regions(teachingTemplateBalls, out HObject expectBallRegions);
            HOperatorSet.Union1(expectBallRegions, out HObject expectBallRegionUnion);
            expectBallRegions.Dispose();
            HOperatorSet.SelectShapeProto(ballRegions, expectBallRegionUnion, out HObject noExtraBallRegion, "overlaps_abs", 1, 1e10);
            expectBallRegionUnion.Dispose();

            VisionOperation.Difference(ballRegions, noExtraBallRegion, out extraBallRegion);
            noExtraBallRegion.Dispose();

            int count = VisionOperation.GetCountOf(extraBallRegion);
            EResultType type = count == 0 ? EResultType.Good : EResultType.ExtraBall;

            return new Result<int>(type, count);
        }

        public static Result<int> InspectBallBridging(HObject image, List<Roi> ballRois, List<Circle> templateBalls, Threshold ballThreshold, double ballMinArea, double ballMaxArea, out HObject ballBridgingRegion)
        {
            VisionOperation.ReduceDomain(image, ballRois, out HObject ballRoiImage);
            VisionOperation.Threshold(ballRoiImage, ballThreshold, out HObject region);
            HOperatorSet.Connection(region, out HObject connectedRegions);
            region.Dispose();
            HOperatorSet.SelectShape(connectedRegions, out HObject regions, "area", "and", ballMinArea, 1e10);
            connectedRegions.Dispose();

            HTuple rows = new HTuple(templateBalls.Select(b => b.Y).ToArray());
            HTuple columns = new HTuple(templateBalls.Select(b => b.X).ToArray());
            HOperatorSet.GenEmptyObj(out ballBridgingRegion);

            for (int i = 0; i < VisionOperation.GetCountOf(regions); i++)
            {
                HOperatorSet.SelectObj(regions, out HObject selectedRegion, i + 1);

                // 각각의 Region에 templateBalls의 중심점이 2개 이상 포함되어 있으면 Bridging
                HOperatorSet.TestRegionPoints(selectedRegion, rows, columns, out HTuple isInsideArray);
                HOperatorSet.TupleSum(isInsideArray, out HTuple sum);
                if (sum.D >= 2)
                {
                    HOperatorSet.ConcatObj(ballBridgingRegion, selectedRegion, out ballBridgingRegion);
                }

                selectedRegion.Dispose();
            }

            regions.Dispose();

            int count = VisionOperation.GetCountOf(ballBridgingRegion);
            EResultType type = count == 0 ? EResultType.Good : EResultType.BallBridging;
            return new Result<int>(type, count);
        }

        public static Result<StatisticalList<Length>> InspectBallSize(HObject ballRegions, Dictionary<string, List<Circle>> ballsByRoi, Dictionary<string, double> templateBallDiameterByRoi, out HObject wrongSizeBallRegion)
        {
            EResultType type = EResultType.Good;
            ECamera cameraType = ECamera.PRS;

            double tolerance = GlobalSetting.Instance.Inspection.Tolerance.BgaBallSizeDiameter;
            HOperatorSet.GenEmptyRegion(out wrongSizeBallRegion);

            StatisticalList<Length> ballDiameters = new StatisticalList<Length>();

            foreach ((string roiName, List<Circle> balls) in ballsByRoi)
            {
                foreach (Circle ball in balls)
                {
                    double diameterPx = ball.Radius * 2;
                    Length diameter = new Length(diameterPx).ConvertFromPixel(cameraType);

                    ballDiameters.Add(diameter);

                    if (Math.Abs(diameter.Value - templateBallDiameterByRoi[roiName]) < tolerance)
                    {
                        continue;
                    }

                    type = EResultType.BallSize;
                    VisionOperation.SelectRegionsByPoints(ballRegions, ball.Y, ball.X, out HObject selectedRegion);
                    HOperatorSet.ConcatObj(wrongSizeBallRegion, selectedRegion, out wrongSizeBallRegion);
                    selectedRegion.Dispose();
                }
            }

            return new Result<StatisticalList<Length>>(type, ballDiameters);
        }

        public static Result<int> InspectBallPosition(HObject ballRegions, List<Circle> foundBalls, List<Circle> templateBalls, out HObject wrongPositionBallRegion)
        {
            ECamera cameraType = ECamera.PRS;
            Point tolerance = GlobalSetting.Instance.Inspection.Tolerance.BgaBallPosition;
            HOperatorSet.GenEmptyRegion(out wrongPositionBallRegion);

            KdTree<double, int> kdTree = new KdTree<double, int>(2, new DoubleMath());
            for (int index = 0; index < foundBalls.Count; ++index)
            {
                kdTree.Add(new[] { foundBalls[index].X, foundBalls[index].Y }, index);
            }

            foreach (Circle templateBall in templateBalls)
            {
                double kdTreeSearchRadius = templateBall.Radius * 2; // 2*Radius 거리(pitch) 내에 볼을 찾음
                KdTreeNode<double, int>[]? nodes = kdTree.RadialSearch(new[] { templateBall.X, templateBall.Y }, kdTreeSearchRadius);

                if (nodes.Length == 0)
                {
                    continue; // 2*Radius 거리(pitch) 내에 볼이 없어 검사할 수 없음
                }

                int nearestCircleIndex = nodes.First().Value;
                Circle nearestCircle = foundBalls[nearestCircleIndex];

                Length xOffset = new Length(nearestCircle.X - templateBall.X).ConvertFromPixel(cameraType);
                Length yOffset = new Length(nearestCircle.Y - templateBall.Y).ConvertFromPixel(cameraType);

                Length xTolerance = new Length(tolerance.Col);
                Length yTolerance = new Length(tolerance.Row);
                bool ngCondition = xOffset.Abs() > xTolerance || yOffset.Abs() > yTolerance;
                if (!ngCondition)
                {
                    continue;
                }

                VisionOperation.SelectRegionsByPoints(ballRegions, nearestCircle.Y, nearestCircle.X, out HObject selectedRegion);
                HOperatorSet.ConcatObj(wrongPositionBallRegion, selectedRegion, out wrongPositionBallRegion);
                selectedRegion.Dispose();
            }

            int count = VisionOperation.GetCountOf(wrongPositionBallRegion);
            EResultType type = count == 0 ? EResultType.Good : EResultType.BallPosition;
            return new Result<int>(type, count);
        }

        public static Result<int> InspectBallCount(HObject ballRegions, int templateBallsCount)
        {
            HOperatorSet.Union1(ballRegions, out HObject ballRegionUnion);
            HOperatorSet.Connection(ballRegionUnion, out HObject regions);
            ballRegionUnion.Dispose();

            int count = VisionOperation.GetCountOf(regions);
            regions.Dispose();

            EResultType type = count == templateBallsCount ? EResultType.Good : EResultType.BallCount;
            return new Result<int>(type, count);
        }

        public static Result<StatisticalList<Length>> InspectBallPitch(List<Roi> rois, Dictionary<string, List<Circle>> ballsByRoi, HObject ballRegions, Dictionary<string, Length> expectedPitchByRoi, out HObject wrongPitchRegions, out HObject edgeRegion, double minAngle = 20.0, double maxAngle = 70.0)
        {
            ECamera cameraType = ECamera.PRS;
            EResultType errorType = EResultType.BallPitch;
            Length tolerance = new Length(GlobalSetting.Instance.Inspection.Tolerance.BgaBallPitch);

            return VisionEngine.InspectPitch(rois, ballRegions, expectedPitchByRoi, tolerance, cameraType, errorType, out wrongPitchRegions, out edgeRegion, minAngle, maxAngle);
        }


        public static Result<int> CrackBall(HObject image, out HObject crakBallRegion)
        {
            HOperatorSet.GenEmptyObj(out crakBallRegion);

            int count = VisionOperation.GetCountOf(crakBallRegion);
            EResultType type = count == 0 ? EResultType.Good : EResultType.CrackBall;
            return new Result<int>(type, count);
        }
    }
}