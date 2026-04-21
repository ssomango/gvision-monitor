using GVisionWpf.Visions;

namespace GVisionWpf.DomainLayer.Extensions
{
    public static class HObjectExtensions
    {
        public static HObject ReduceDomain(this HObject source, HObject region)
        {
            HOperatorSet.Union1(region, out HObject regionUnion);
            HOperatorSet.ReduceDomain(source, regionUnion, out HObject reducedImage);
            return reducedImage;
        }

        public static HObject ReduceDomain(this HObject image, Roi roi)
        {
            HObject region = roi.Roi2Region();
            return image.ReduceDomain(region);
        }


        public static HObject CopyImage(this HObject source)
        {
            HOperatorSet.CopyImage(source, out HObject dupImage);
            return dupImage;
        }

        public static HObject AffineTransformRegion(this HObject source, HTuple homMat2D)
        {
            if (source == null || source.IsInitialized() == false || source.CountObj() == 0)
                return new HObject();

            HTuple type;
            HOperatorSet.GetObjClass(source, out type);

            if (type.S == "xld_cont")
            {
                var transformXld = source.AffineTransformContourXld(homMat2D);
                return transformXld;
            }

            HOperatorSet.AffineTransRegion(source, out HObject transformedRegion, homMat2D, "constant");
            return transformedRegion;
        }

        public static HObject AffineTransformImage(this HObject source, HTuple homMat2D)
        {
            HOperatorSet.AffineTransImage(source, out HObject resultImage, homMat2D, "constant", "false");
            return resultImage;
        }

        public static HObject AffineTransformContourXld(this HObject source, HTuple homMat2D)
        {
            HOperatorSet.AffineTransContourXld(source, out HObject result, homMat2D);
            return result;
        }


        public static HObject CropDomain(this HObject source)
        {
            HOperatorSet.CropDomain(source, out HObject imagePart);
            return imagePart;
        }

        public static Roi Region2Roi(this HObject source)
        {
            HOperatorSet.SmallestRectangle1(source, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
            return new Roi("", row1, col1, row2, col2);
        }

        public static List<Roi> Region2Rois(this HObject source)
        {
            List<Roi> rois = new List<Roi>(256);

            HOperatorSet.Connection(source, out source);

            for (int i = 1; i <= source.CountObj(); i++)
            {
                HOperatorSet.SelectObj(source, out HObject selectedRegion, i);

                using (selectedRegion)
                {
                    Roi roi = selectedRegion.Region2Roi();

                    rois.Add(roi);
                }
            }

            return rois;
        }

        public static HObject OmitRegionFromTarget(this HObject source, List<Roi> rois, double dilation)
        {
            HObject regions = rois.Rois2Regions();

            if (dilation > 0)
            {
                HOperatorSet.DilationCircle(regions, out regions, dilation);
            }

            VisionOperation.ReduceDomainComplement(source, regions, out HObject resultImage);

            regions.Dispose();
            return resultImage;
        }

        public static HObject OmitRegionFromTarget(this HObject source, HObject region, double dilation)
        {
            HOperatorSet.DilationCircle(region, out HObject regionDilation, dilation);
            VisionOperation.ReduceDomainComplement(source, regionDilation, out HObject resultImage);

            regionDilation.Dispose();
            return resultImage;
        }

        public static bool IsEmpty(this HObject source, DisposeBag disposeBag)
        {
            HTuple value;

            HOperatorSet.RegionFeatures(source, "area", out value);

            using (value)
            {
                if (value == null || value <= 0 || value.Type == HTupleType.EMPTY)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
