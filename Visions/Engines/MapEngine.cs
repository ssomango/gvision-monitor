using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Repositories;

namespace GVisionWpf.Visions.Engines
{
    public class MapEngine : VisionEngine
    {
        public static List<HObject> FindPackageRegions(HObject image, Roi packageTotalRoi, int row, int col, EEdgeDetectDirection direction, EEdgeDetectMode detectMode, int thresholdDiff)
        {
            VisionOperation.PartitionRectangle(packageTotalRoi, row, col, out HObject partition);
            VisionOperation.Region2Rois(partition, out List<Roi> rois);

            List<HObject> regions = new List<HObject>(64);
            foreach (Roi roi in rois)
            {
                try
                {
                    VisionOperation.Roi2BorderBoxes(roi, out Roi top, out Roi bottom, out Roi left, out Roi right);
                    GetPackageRegion(image, top, bottom, left, right, direction, detectMode, thresholdDiff, out HObject region, out _);

                    regions.Add(region);
                }
                catch
                {
                    // ignored
                }
            }

            return regions;
        }

        public static void AlignImageWithPose(HObject image, HObject packageRegion, Pose modelPose, out HObject alignedImage, out HTuple homMat2D, out HTuple invHomMat2D, out HTuple composedHomMat2D)
        {
            HOperatorSet.SetSystem("width", 4096);
            HOperatorSet.SetSystem("height", 4096);
            VisionOperation.GetRegionOrientationOfSmallestRectangle2(packageRegion, out Pose pxPose, out _);

            HOperatorSet.VectorAngleToRigid(pxPose.Y, pxPose.X, new HTuple(pxPose.T).TupleRad(), modelPose.Y, modelPose.X, new HTuple(modelPose.T).TupleRad(), out homMat2D);
            VisionOperation.AffineTransformImage(image, homMat2D, out alignedImage);

            HOperatorSet.VectorAngleToRigid(modelPose.Y, modelPose.X, new HTuple(modelPose.T).TupleRad(), pxPose.Y, pxPose.X, new HTuple(pxPose.T).TupleRad(), out invHomMat2D);
            HOperatorSet.HomMat2dCompose(homMat2D, invHomMat2D, out composedHomMat2D);

            VisionOperation.AffineTransformRegion(packageRegion, homMat2D, out HObject alignedPackageRegion);
            HOperatorSet.ReduceDomain(alignedImage, alignedPackageRegion, out alignedImage);
            alignedPackageRegion.Dispose();
        }

        public static void PartitionRoi(Roi roi, int nRow, int nCol, out List<Roi> rois)
        {
            VisionOperation.PartitionRectangle(roi, nRow, nCol, out HObject partitionRegion);
            HOperatorSet.SortRegion(partitionRegion, out partitionRegion, "character", "true", "row");
            VisionOperation.Region2Rois(partitionRegion, out rois);
            partitionRegion.Dispose();
        }

        public static HTuple TrainMarkPattern(HObject croppedImage, Threshold threshold)
        {
            // 경험적으로 확인한 결과, 바이너라이제이션 해주는게 20% 정도 높은 매칭률
            HObject region = VisionOperation.GetConnectedTextRegion(croppedImage, threshold);

            if (VisionOperation.IsEmpty(region))
            {
                throw new BlobNotFoundException();
            }

            HOperatorSet.Union1(region, out region); // 안해도 되나?
            HOperatorSet.GetImageSize(croppedImage, out HTuple width, out HTuple height);
            HOperatorSet.RegionToBin(region, out croppedImage, 255, 0, width, height);

            HOperatorSet.CreateGenericShapeModel(out HTuple model);
            HOperatorSet.SetGenericShapeModelParam(model, "num_levels", 3);
            HOperatorSet.SetGenericShapeModelParam(model, "max_deformation", 0);
            //HOperatorSet.SetGenericShapeModelParam(model, "border_shape_models", "true");
            HOperatorSet.TrainGenericShapeModel(croppedImage, model);

            return model;
        }

        public static bool InspectMark(HObject image, MarkItem mark, HTuple ocrHandle, out string ocredText)
        {
            if (mark.Mode == EMarkMode.Ocr)
            {
                ocredText = VisionOperation.GetOcredText(image, mark.connectedTextRegion, ".*", ocrHandle);

                if (ocredText != mark.OcrText)
                {
                    return false;
                }
            }
            else
            {
                // TODO: generic shape model 사용, 할콘 first example shape matching 참고
                ocredText = "Match: ";

                HOperatorSet.FindGenericShapeModel(image, mark.ShapeMatchingModel, out HTuple MatchResultID,
                    out HTuple NumMatchResult);

                if (NumMatchResult.I == 0)
                {
                    ocredText += "0%";
                    return false;
                }

                HOperatorSet.GetGenericShapeModelResultObject(out HObject foundContours, MatchResultID, "all", "contours");
                HOperatorSet.GetGenericShapeModelResult(MatchResultID, "all", "score", out HTuple score);

                ocredText += score + "%";

                if (score.D * 100 <= mark.MinMatchingRate)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool InspectMatchingMark(HObject image, MarkItem mark, out double matchingRate)
        {
            // TODO: generic shape model 사용, 할콘 first example shape matching 참고
            HOperatorSet.FindGenericShapeModel(image, mark.ShapeMatchingModel, out HTuple MatchResultID, out HTuple NumMatchResult);

            if (NumMatchResult.I == 0)
            {
                matchingRate = 0;
                return false;
            }

            // HOperatorSet.GetGenericShapeModelResultObject(out HObject foundContours, MatchResultID, "all", "contours");
            HOperatorSet.GetGenericShapeModelResult(MatchResultID, "all", "score", out HTuple score);

            matchingRate = score.D * 100;

            if (matchingRate <= mark.MinMatchingRate)
            {
                return false;
            }

            return true;
        }

        public static bool InspectOcrMark(HObject image, MarkItem mark, HTuple ocrHandle, out string ocredText)
        {
            ocredText = VisionOperation.GetOcredText(image, mark.connectedTextRegion, ".*", ocrHandle);

            return ocredText == mark.OcrText;
        }

        public static int CountCharacter(MarkItem mark, out HObject textBoxs)
        {
            HOperatorSet.GenEmptyObj(out textBoxs);

            if (!VisionOperation.IsEmpty(mark.connectedTextRegion))
            {
                HOperatorSet.SmallestRectangle1(mark.connectedTextRegion, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                HOperatorSet.GenRectangle1(out textBoxs, row1, col1, row2, col2);
                HOperatorSet.Union1(textBoxs, out textBoxs);
                HOperatorSet.Connection(textBoxs, out textBoxs);
            }

            return VisionOperation.GetCountOf(textBoxs);
        }

        public static Result<string> InspectDataCode(HObject image, Roi roi, HTuple dataCodeHandle, out HObject symbolXLDs)
        {
            // TODO: 못찾은 경우 어떻게 되는지 파악해서 변경 필요
            string decodedText = VisionOperation.GetTextFromDataCode(image, roi, dataCodeHandle, out symbolXLDs);
            return decodedText == "" ? new Result<string>(EResultType.DataCode, "DECODING FAIL") : new Result<string>(EResultType.Good, decodedText);
        }

        public static Result<Pose> InspectTextOffset(Pose packagePose, List<MarkItem> markItems, Pose taughtTextOffset)
        {
            EResultType type = EResultType.Good;

            HOperatorSet.GenEmptyRegion(out HObject markRegions);

            foreach (MarkItem markItem in markItems)
            {
                HOperatorSet.Union2(markRegions, markItem.connectedTextRegion, out markRegions);
            }

            VisionOperation.GetRegionOrientationOfSmallestRectangle2(markRegions, out Pose markPose, out _);

            Pose textOffset = markPose - packagePose;
            Pose diffOffset = taughtTextOffset - textOffset;

            Pose convertedDiffOffset = diffOffset.ConvertFromPixel(ECamera.Mapping);

            if (Math.Abs(convertedDiffOffset.X) > GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetX || Math.Abs(convertedDiffOffset.Y) > GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetY)
            {
                type = EResultType.TextOffset;
            }

            return new Result<Pose>(type, convertedDiffOffset);
        }

        public static Result<Size> InspectPackageSize(HObject packageRegion, out FixedText text)
        {
            return VisionEngine.InspectPackageSize(packageRegion, DeviceRecipeRepository.Instance.GetRecipe().PackageSize, GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize, ECamera.Mapping, out text);
        }

        public static void TeachMarks(HObject image, IMarkTeachingModel<InspectionTeaching> teaching, HObject teachingPackageRegion, out HObject smallestMarkRegion, out HObject packageReticle, out HObject markReticle, out Pose markPose, out Pose packagePose)
        {
            // teaching.MarkItems.Clear();
            HOperatorSet.GenEmptyRegion(out HObject markRegions);

            List<MarkItem> tmp = new List<MarkItem>();

            foreach (MarkItem markItem in teaching.MarkItems)
            {
                VisionOperation.ReduceDomain(image, markItem.Roi, out HObject markImage);
                HObject connectedRegion = VisionOperation.GetConnectedTextRegion(markImage, teaching.MarkThreshold);

                if (markItem.Mode == EMarkMode.ShapeMatching)
                {
                    HOperatorSet.CropDomain(markImage, out HObject matchingImage);
                    markItem.ShapeMatchingModel = MapEngine.TrainMarkPattern(matchingImage, teaching.MarkThreshold);
                }
                else if (markItem.Mode == EMarkMode.Ocr)
                {
                    HOperatorSet.ReadOcrClassCnn("Universal_0-9A-Z_NoRej.occ", out HTuple ocrHandle);
                    markItem.OcrText = VisionOperation.GetOcredText(image, connectedRegion, ".*", ocrHandle);
                }

                HOperatorSet.Union2(markRegions, connectedRegion, out markRegions);

                markItem.nCharacters = VisionOperation.GetCountOf(connectedRegion);

                tmp.Add(markItem);
            }

            teaching.MarkItems = tmp;

            VisionOperation.GetRegionOrientationOfSmallestRectangle2(teachingPackageRegion, out packagePose, out _);
            VisionOperation.GetRegionOrientationOfSmallestRectangle2(markRegions, out markPose, out _);
            VisionOperation.Region2Roi(markRegions, out Roi smallestMarkRegionRoi);
            VisionOperation.Roi2Region(smallestMarkRegionRoi, out smallestMarkRegion);

            VisionOperation.GetCenterPointOfRegion(image, out Point imageCenterPoint);
            double reticleSize = imageCenterPoint.Col / 30;

            VisionOperation.GenReticle(packagePose, reticleSize, out packageReticle);
            VisionOperation.GenReticle(markPose, reticleSize, out markReticle);

            Pose pxTextOffset = markPose - packagePose;
            teaching.TextOffset = pxTextOffset;
        }

        public static Result<SawOffset> InspectNewSawOffset(SawOffsetItem sawOffsetItem, List<Point> packagePoints, HObject firstPinRegion, HObject markRegion, out HObject footOfPerpendicular)
        {
            // TODO: 언젠간 타겟리전 똑바로 구해서 넣어주겠지 뭐
            Dictionary<ESawOffsetStandardObject, HObject> targetDictionary = new Dictionary<ESawOffsetStandardObject, HObject>
            {
                [ESawOffsetStandardObject.FirstPin] = firstPinRegion,
                [ESawOffsetStandardObject.Mark] = markRegion
            };

            List<SawOffsetItem> teachingSawOffsetItems = new List<SawOffsetItem> { sawOffsetItem };

            return VisionEngine.InspectSawOffset(teachingSawOffsetItems, packagePoints, targetDictionary, GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX, GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX, ECamera.Mapping, out footOfPerpendicular);
        }

        public static Result<CornerDegree> InspectCornerDegree(List<Point> points, out HObject cornerPoint, out List<FloatingText> texts)
        {
            double tolerance = GlobalSetting.Instance.Inspection.Tolerance.MapCornerDegree;
            return VisionEngine.InspectCornerDegree(points, tolerance, out cornerPoint, out texts);
        }
    }
}