using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Visions.Engines
{
    public class CalibrationEngine : VisionEngine
    {
        public static CalibrationResult CalibrationEngineBase(HObject image, Roi roi, Threshold threshold, int minSize, int maxSize, double similarity, EShape shape, ECalibrationStandard standardType, ECalibration calibrationType, ECamera cameraType)
        {
            HObject pointRegion = new HObject();
            int count = 0;
            Pose targetPose = new Pose(0, 0, 0);
            List<Pose> poses = new List<Pose>();

            VisionOperation.GetImageMidPoint(image, out Point imageCenterPoint);

            VisionOperation.Roi2Region(roi, out HObject roiRegion);
            VisionOperation.ReduceDomain(image, roiRegion, out HObject reducedImage);
            roiRegion.Dispose();

            switch (shape)
            {
                case EShape.Circle:
                    VisionOperation.FindCircles(reducedImage, threshold, minSize, maxSize, similarity, out pointRegion, out List<Circle> circles);
                    count = circles.Count;
                    foreach (Circle circle in circles)
                    {
                        poses.Add(new Pose(circle.X, circle.Y, 0));
                    }

                    break;
                case EShape.Rectangle:
                    VisionOperation.FindRectangles(reducedImage, threshold, minSize, maxSize, similarity, out pointRegion, out List<Rect2> rectangles);
                    count = rectangles.Count;
                    foreach (Rect2 rectangle in rectangles)
                    {
                        poses.Add(new Pose(rectangle.Col, rectangle.Row, VisionOperation.Radian2DegreeForOffset(rectangle.Phi)));
                    }

                    break;
            }

            reducedImage.Dispose();

            CalibrationResult result = new CalibrationResult
            {
                Image = image,
                ImageCenterPoint = imageCenterPoint,
                CalibrationType = calibrationType,
                CameraType = cameraType,
                Region = pointRegion,
                IsFound = false,
            };

            if (count == 0)
            {
                return result;
            }

            switch (standardType)
            {
                case ECalibrationStandard.MultiObject:
                    double xSum = 0, ySum = 0;
                    foreach (Pose point in poses)
                    {
                        xSum += point.X;
                        ySum += point.Y;
                    }

                    targetPose.X = xSum / count;
                    targetPose.Y = ySum / count;
                    break;

                case ECalibrationStandard.Center:
                    VisionOperation.GetMostMidRegion(pointRegion, imageCenterPoint, out HObject mostMidRegion);
                    VisionOperation.FindCenterPose(poses, imageCenterPoint, out targetPose);
                    result.Region = mostMidRegion;
                    break;

                case ECalibrationStandard.Biggest:
                    VisionOperation.GetMaxAreaRegion(pointRegion, out HObject maxRegion);
                    foreach (Pose pose in poses)
                    {
                        if (VisionOperation.IsPointInRegion(new Point(pose.Y, pose.X), maxRegion))
                        {
                            targetPose = pose;
                            break;
                        }
                    }

                    result.Region = maxRegion;
                    break;
            }

            Pose pxOffset = new Pose(
                x: targetPose.X - imageCenterPoint.Col,
                y: targetPose.Y - imageCenterPoint.Row,
                t: targetPose.T
            );
            Pose offset = pxOffset.ConvertFromPixel(cameraType);

            result.IsFound = true;
            result.TargetPose = targetPose;
            result.Offset = offset;

            return result;
        }
    }
}