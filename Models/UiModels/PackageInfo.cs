namespace GVisionWpf.Models.UiModels
{
    public class PackageInfo
    {
        public PackageInfo(HObject packageRegion, List<Point> packagePoints)
        {
            this.PackageRegion = packageRegion;
            this.PackagePoints = packagePoints;
        }

        public HObject? PackageRegion;
        public List<Point>? PackagePoints;
    }

}
