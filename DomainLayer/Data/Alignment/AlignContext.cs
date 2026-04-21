namespace GVisionWpf.DomainLayer.Data.Alignment
{
    public class AlignContext : IDisposable
    {
        public HObject AlignedImage = new HObject();

        public HObject PackageRegion = new HObject();
        
        public List<Point> PackagePoints = new List<Point>();

        public HTuple TransformMatrix = new HTuple();

        public HTuple TransformMatrixInvert = new HTuple();

        public void Dispose()
        {
            AlignedImage.Dispose();
            PackageRegion.Dispose();
            TransformMatrix.Dispose();
            TransformMatrixInvert.Dispose();
        }
    }
}
