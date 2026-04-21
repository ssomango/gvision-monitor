using GVisionWpf.GlobalStates;
using GVisionWpf.Repositories;

namespace GVisionWpf.Visions.Engines
{
    public class QfnEngine : VisionEngine
    {
        public static void FindLeads(HObject image, List<Roi> rois, Threshold threshold, out HObject region)
        {
            VisionOperation.ReduceDomain(image, rois, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();

            HOperatorSet.OpeningCircle(region, out region, 2);
            HOperatorSet.Connection(region, out region);
        }

        public static void FindPad(HObject image, Roi roi, Threshold threshold, out HObject region)
        {
            VisionOperation.ReduceDomain(image, roi, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();

            HOperatorSet.Connection(region, out region);
            HOperatorSet.SelectShapeStd(region, out region, "max_area", 70);
        }

        public static Result<StatisticalList<Size>> InspectLeadSizes(HObject leadsRegion, Size originalSize)
        {
            StatisticalList<Size> sizes = new StatisticalList<Size>();
            EResultType type = EResultType.Good;

            for (int i = 1; i <= leadsRegion.CountObj(); i++)
            {
                VisionOperation.GetRegionOrientationOfSmallestRectangle2(leadsRegion[i], out _, out Size pxSize);

                if (pxSize.Width > pxSize.Height) // INTENTION: 가로가 세로보다 길 경우, 가로와 세로를 바꿔줌
                {
                    pxSize = new Size(pxSize.Height, pxSize.Width);
                }

                Size size = pxSize.ConvertFromPixel(ECamera.PRS);
                sizes.Add(size);

                Size tolerance = GlobalSetting.Instance.Inspection.Tolerance.QfnLeadSize;
                if (Math.Abs(size.Width - originalSize.Width) > tolerance.Width || Math.Abs(size.Height - originalSize.Height) > tolerance.Height)
                {
                    type = EResultType.LeadSize;
                }
            }

            return new Result<StatisticalList<Size>>(type, sizes);
        }

        public static Result<List<LengthStatsInRoi>> InspectLeadPitches(HObject leadsRegion, List<Roi> leadRois)
        {
            List<LengthStatsInRoi> pitchStatsInRois = new List<LengthStatsInRoi>(leadRois.Count);
            foreach (Roi leadRoi in leadRois)
            {
                VisionOperation.Roi2Region(leadRoi, out HObject roiRegion);
                HOperatorSet.Intersection(roiRegion, leadsRegion, out HObject leadRegion);
                HOperatorSet.Connection(leadRegion, out leadRegion);
                VisionOperation.Distance(leadRegion, out List<Length> pxPitches);
                leadRegion.Dispose();

                LengthStatsInRoi pitchStatsInRoi = new LengthStatsInRoi(leadRoi.Name);
                foreach (Length pxPitch in pxPitches)
                {
                    Length pitch = pxPitch.ConvertFromPixel(ECamera.PRS);
                    pitchStatsInRoi.Add(pitch);
                }
                pitchStatsInRois.Add(pitchStatsInRoi);
            }

            return new Result<List<LengthStatsInRoi>>(EResultType.Good, pitchStatsInRois);
        }

        public static Result<Size> InspectPadSize(HObject padRegion, Size originalSize)
        {
            double toleranceWidth = GlobalSetting.Instance.Inspection.Tolerance.QfnPadSizeWidth;
            double toleranceHeight = GlobalSetting.Instance.Inspection.Tolerance.QfnPadSizeHeight;
            VisionOperation.GetRegionOrientationOfSmallestRectangle2(padRegion, out _, out Size pxSize);
            Size size = pxSize.ConvertFromPixel(ECamera.PRS);

            EResultType type = EResultType.Good;
            if (Math.Abs(size.Width - originalSize.Width) > toleranceWidth || Math.Abs(size.Height - originalSize.Height) > toleranceHeight)
            {
                type = EResultType.PadSize;
            }

            return new Result<Size>(type, size);
        }

        public static Result<StatisticalList<Ratio>> InspectLeadArea(HObject region, int leadAvgArea)
        {
            return InspectAreas(region, leadAvgArea, GlobalSetting.Instance.Inspection.Tolerance.QfnLeadArea, EResultType.LeadArea);
        }

        public static Result<Ratio> InspectPadArea(HObject region, int padArea)
        {
            return InspectArea(region, padArea, GlobalSetting.Instance.Inspection.Tolerance.QfnPadArea, EResultType.PadArea);
        }

        public static Result<StatisticalList<Length>> InspectLeadPerimeter(HObject region, double avgPerimeter, out HObject errorRegion)
        {
            HOperatorSet.GenEmptyRegion(out errorRegion);
            StatisticalList<Length> perimeters = new StatisticalList<Length>();
            EResultType type = EResultType.Good;

            for (int i = 1; i <= region.CountObj(); i++)
            {
                VisionOperation.GetRegionPerimeter(region[i], out Length pxPerimeter);
                Length perimeter = pxPerimeter.ConvertFromPixel(ECamera.PRS);
                perimeters.Add(perimeter);

                if (Math.Abs(avgPerimeter - perimeter.Value) > GlobalSetting.Instance.Inspection.Tolerance.QfnLeadPerimeter)
                {
                    type = EResultType.LeadPerimeter;
                    errorRegion.ConcatObj(region[i]);
                }
            }

            return new Result<StatisticalList<Length>>(type, perimeters);
        }

        public static Result<StatisticalList<Pose>> InspectLeadOffset(HObject region, List<Pose> centerPxPoses, out HObject errorRegion)
        {
            return VisionEngine.InspectOffsets(region, centerPxPoses, GlobalSetting.Instance.Inspection.Tolerance.QfnLeadOffset, ECamera.PRS, EResultType.LeadOffset, out errorRegion);
        }

        public static Result<Size> InspectPackageSize(HObject packageRegion, out FixedText text)
        {
            return VisionEngine.InspectPackageSize(packageRegion, DeviceRecipeRepository.Instance.GetRecipe().PackageSize, GlobalSetting.Instance.Inspection.Tolerance.QfnPackageSize, ECamera.PRS, out text);
        }

        public static Result<int> InspectLeadContamination(HObject leadRegion, int minSize, int maxSize, out HObject errorRegion)
        {
            // TODO: 리드 컨타미네이션 검사에 대한 정의가 이루어지지 않음
            HOperatorSet.GenEmptyRegion(out errorRegion);
            return new Result<int>();
        }

        public static Result<SawOffset> InspectNewSawOffset(List<SawOffsetItem> teachingSawOffsetItems, List<Point> packagePoints, HObject leadRegion, HObject padRegion, out HObject footOfPerpendicular)
        {
            Dictionary<ESawOffsetStandardObject, HObject> targetDictionary = new Dictionary<ESawOffsetStandardObject, HObject>
            {
                [ESawOffsetStandardObject.Pad] = padRegion,
                [ESawOffsetStandardObject.Lead] = leadRegion
            };

            return VisionEngine.InspectSawOffset(teachingSawOffsetItems, packagePoints, targetDictionary, GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetX, GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetY, ECamera.PRS, out footOfPerpendicular);
        }

        public static Result<CornerDegree> InspectCornerDegree(List<Point> points, out HObject cornerPoint, out List<FloatingText> texts)
        {
            return VisionEngine.InspectCornerDegree(points, GlobalSetting.Instance.Inspection.Tolerance.QfnCornerDegree, out cornerPoint, out texts);
        }
    }
}