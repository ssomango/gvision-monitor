namespace GVisionWpf.DomainLayer.Extensions
{
    public static class RoiExtensions
    {
        public static HObject Roi2Region(this Roi source)
        {
            HOperatorSet.GenRectangle1(out HObject rectangle, source.Row1, source.Col1, source.Row2, source.Col2);

            return rectangle;
        }

        public static HObject Rois2Regions(this List<Roi> sources)
        {
            HOperatorSet.GenEmptyRegion(out HObject rectangle);

            foreach (var source in sources)
            {
                HObject region;
                HOperatorSet.GenRectangle1(out region, source.Row1, source.Col1, source.Row2, source.Col2);
                HOperatorSet.Union2(rectangle, region, out rectangle);
            }

            return rectangle;
        }
    }
}
