using GVisionWpf.GlobalStates;
using GVisionWpf.Repositories;

namespace GVisionWpf.Visions.Engines
{
    public class LgaEngine : VisionEngine
    {
        public static void FindLeads(HObject image, List<Roi> rois, Threshold threshold, out HObject region)
        {
            VisionOperation.ReduceDomain(image, rois, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();

            HOperatorSet.OpeningCircle(region, out region, 2);
            HOperatorSet.Connection(region, out region);
        }

        public static Result<StatisticalList<Size>> InspectLeadSizes(HObject leadsRegion, Size originalSize)
        {
            StatisticalList<Size> sizes = new StatisticalList<Size>();
            EResultType type = EResultType.Good;

            for (int i = 1; i <= leadsRegion.CountObj(); i++)
            {
                VisionOperation.GetRegionOrientationOfSmallestRectangle2(leadsRegion[i], out _, out Size pxSize);
                Size size = pxSize.ConvertFromPixel(ECamera.PRS);
                sizes.Add(size);

                Size tolerance = GlobalSetting.Instance.Inspection.Tolerance.LgaLeadSize;
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

        public static Result<StatisticalList<Ratio>> InspectLeadArea(HObject region, int leadAvgArea)
        {
            return InspectAreas(region, leadAvgArea, GlobalSetting.Instance.Inspection.Tolerance.LgaLeadArea, EResultType.LeadArea);
        }

        public static Result<StatisticalList<Length>> InspectLeadPerimeter(HObject region, double avgPerimeter, out HObject errorRegion)
        {
            HOperatorSet.GenEmptyRegion(out errorRegion);
            StatisticalList<Length> lengths = new StatisticalList<Length>();
            EResultType type = EResultType.Good;

            for (int i = 1; i <= region.CountObj(); i++)
            {
                VisionOperation.GetRegionPerimeter(region[i], out Length pxPerimeter);
                Length perimeter = pxPerimeter.ConvertFromPixel(ECamera.PRS);
                lengths.Add(perimeter);

                if (Math.Abs(avgPerimeter - perimeter.Value) > GlobalSetting.Instance.Inspection.Tolerance.LgaLeadPerimeter)
                {
                    type = EResultType.LeadPerimeter;
                    errorRegion.ConcatObj(region[i]);
                }
            }

            return new Result<StatisticalList<Length>>(type, lengths);
        }

        public static Result<StatisticalList<Pose>> InspectLeadOffset(HObject region, List<Pose> centerPxPoses, out HObject errorRegion)
        {
            return VisionEngine.InspectOffsets(region, centerPxPoses, GlobalSetting.Instance.Inspection.Tolerance.LgaLeadOffset, ECamera.PRS, EResultType.LeadOffset, out errorRegion);
        }

        public static Result<Size> InspectPackageSize(HObject packageRegion, out FixedText text)
        {
            return VisionEngine.InspectPackageSize(packageRegion, DeviceRecipeRepository.Instance.GetRecipe().PackageSize, GlobalSetting.Instance.Inspection.Tolerance.LgaPackageSize, ECamera.PRS, out text);
        }

        public static Result<int> InspectLeadContamination(HObject leadRegion, int minSize, int maxSize, out HObject errorRegion)
        {
            HOperatorSet.FillUp(leadRegion, out HObject FillupLeadRegion);
            VisionOperation.Difference(FillupLeadRegion, leadRegion, out errorRegion);
            HOperatorSet.Connection(errorRegion, out errorRegion);
            HOperatorSet.SelectShape(errorRegion, out errorRegion, "area", "and", minSize, maxSize);
            HOperatorSet.FillUp(leadRegion, out leadRegion);
            int nLeadContaminations = VisionOperation.IsEmpty(errorRegion) ? 0 : VisionOperation.GetCountOf(errorRegion);

            return new Result<int>(nLeadContaminations == 0 ? EResultType.Good : EResultType.LeadContamination, nLeadContaminations);
        }

        public static void FindMultiPad(HObject image, List<Roi> rois, Threshold threshold, out HObject region)
        {
            VisionOperation.ReduceDomain(image, rois, out HObject reducedImage);
            VisionOperation.Threshold(reducedImage, threshold, out region);
            reducedImage.Dispose();

            HOperatorSet.OpeningCircle(region, out region, 2);
            HOperatorSet.Connection(region, out region);
        }

        public static Result<StatisticalList<Size>> InspectPadSizes(HObject padRegion, Size originalSize)
        {
            StatisticalList<Size> sizes = new StatisticalList<Size>();
            EResultType type = EResultType.Good;

            for (int i = 1; i <= padRegion.CountObj(); i++)
            {
                VisionOperation.GetRegionOrientationOfSmallestRectangle2(padRegion[i], out _, out Size pxSize);
                Size size = pxSize.ConvertFromPixel(ECamera.PRS);
                sizes.Add(size);

                Size tolerance = GlobalSetting.Instance.Inspection.Tolerance.LgaPadSize;
                if (Math.Abs(size.Width - originalSize.Width) > tolerance.Width || Math.Abs(size.Height - originalSize.Height) > tolerance.Height)
                {
                    type = EResultType.MultiPadSize;
                }
            }

            return new Result<StatisticalList<Size>>(type, sizes);
        }

        public static Result<List<LengthStatsInRoi>> InspectMultiPadPitches(HObject multiPadRegion, List<Roi> padRois, ECamera cameraType)
        {
            List<LengthStatsInRoi> pitchStatsInRois = new List<LengthStatsInRoi>(padRois.Count);
            foreach (Roi padRoi in padRois)
            {
                VisionOperation.Roi2Region(padRoi, out HObject roiRegion);
                HOperatorSet.Intersection(roiRegion, multiPadRegion, out HObject padRegion);
                HOperatorSet.Connection(padRegion, out padRegion);
                VisionOperation.Distance(padRegion, out List<Length> pxPitches);
                padRegion.Dispose();

                LengthStatsInRoi pitchStatsInRoi = new LengthStatsInRoi(padRoi.Name);
                foreach (Length pxPitch in pxPitches)
                {
                    Length pitch = pxPitch.ConvertFromPixel(cameraType);
                    pitchStatsInRoi.Add(pitch);
                }
                pitchStatsInRois.Add(pitchStatsInRoi);
            }

            return new Result<List<LengthStatsInRoi>>(EResultType.Good, pitchStatsInRois);
        }

        public static Result<StatisticalList<Length>> InspectPadPerimeter(HObject region, double avgPerimeter, out HObject errorRegion)
        {
            HOperatorSet.GenEmptyRegion(out errorRegion);
            StatisticalList<Length> perimeters = new StatisticalList<Length>();
            EResultType type = EResultType.Good;

            for (int i = 1; i <= region.CountObj(); i++)
            {
                VisionOperation.GetRegionPerimeter(region[i], out Length pxPerimeter);
                Length perimeter = pxPerimeter.ConvertFromPixel(ECamera.PRS);
                perimeters.Add(perimeter);

                if (Math.Abs(avgPerimeter - perimeter.Value) > GlobalSetting.Instance.Inspection.Tolerance.LgaPadPerimeter)
                {
                    type = EResultType.MultiPadPerimeter;
                    errorRegion.ConcatObj(region[i]);
                }
            }

            return new Result<StatisticalList<Length>>(type, perimeters);
        }

        public static Result<StatisticalList<Ratio>> InspectMultiPadArea(HObject region, int multiPadAvgArea)
        {
            return InspectAreas(region, multiPadAvgArea, GlobalSetting.Instance.Inspection.Tolerance.LgaPadArea, EResultType.MultiPadArea);
        }

        public static Result<StatisticalList<Pose>> InspectMultiPadOffset(HObject region, List<Pose> centerPxPoses, out HObject errorRegion)
        {
            return VisionEngine.InspectOffsets(region, centerPxPoses, GlobalSetting.Instance.Inspection.Tolerance.LgaPadOffset, ECamera.PRS, EResultType.MultiPadOffset, out errorRegion);
        }

        public static Result<int> InspectMultiPadContamination(HObject padRegion, int minSize, int maxSize, out HObject errorRegion)
        {
            HOperatorSet.FillUp(padRegion, out HObject FillupMultipadRegion);
            VisionOperation.Difference(FillupMultipadRegion, padRegion, out errorRegion);
            HOperatorSet.Connection(errorRegion, out errorRegion);
            HOperatorSet.SelectShape(errorRegion, out errorRegion, "area", "and", minSize, maxSize);
            HOperatorSet.FillUp(padRegion, out padRegion);
            int nMultiPadContaminations = VisionOperation.IsEmpty(errorRegion) ? 0 : VisionOperation.GetCountOf(errorRegion);

            return new Result<int>(nMultiPadContaminations == 0 ? EResultType.Good : EResultType.MultiPadContamination, nMultiPadContaminations);
        }

        public static Result<SawOffset> InspectNewSawOffset(List<SawOffsetItem> teachingSawOffsetItems, List<Point> packagePoints, HObject leadRegion, HObject padRegion, out HObject footOfPerpendicular)
        {
            Dictionary<ESawOffsetStandardObject, HObject> targetDictionary = new Dictionary<ESawOffsetStandardObject, HObject>
            {
                [ESawOffsetStandardObject.Pad] = padRegion,
                [ESawOffsetStandardObject.Lead] = leadRegion
            };

            return VisionEngine.InspectSawOffset(teachingSawOffsetItems, packagePoints, targetDictionary, GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetX, GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetY, ECamera.PRS, out footOfPerpendicular);
        }

        public static Result<CornerDegree> InspectCornerDegree(List<Point> points, out HObject cornerPoint, out List<FloatingText> texts)
        {
            return VisionEngine.InspectCornerDegree(points, GlobalSetting.Instance.Inspection.Tolerance.LgaCornerDegree, out cornerPoint, out texts);
        }
    }
}