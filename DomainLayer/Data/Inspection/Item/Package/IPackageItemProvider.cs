namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Package
{
    public interface IPackageItemProvider<T> where T : IInspectionItem
    {
        public T NoDevice { get; }

        public T PackageOffset { get; }

        public T PackageSize { get; }
    }

    public class MapPackageProvider : IPackageItemProvider<MoldInspectionItem>
    {
        public MoldInspectionItem NoDevice { get; private set; } = MoldInspectionItem.NoDevice;

        public MoldInspectionItem PackageOffset { get; private set; } = MoldInspectionItem.PackageOffset;

        public MoldInspectionItem PackageSize { get; private set; } = MoldInspectionItem.PackageSize;
    }

    public class BgaPackageProvider : IPackageItemProvider<BgaInspectionItem>
    {
        public BgaInspectionItem NoDevice { get; private set; } = BgaInspectionItem.NoDevice;

        public BgaInspectionItem PackageOffset { get; private set; } = BgaInspectionItem.PackageOffset;

        public BgaInspectionItem PackageSize { get; private set; } = BgaInspectionItem.PackageSize;
    }

    public class LgaPackageProvider : IPackageItemProvider<LgaInspectionItem>
    {
        public LgaInspectionItem NoDevice { get; private set; } = LgaInspectionItem.NoDevice;

        public LgaInspectionItem PackageOffset { get; private set; } = LgaInspectionItem.PackageOffset;

        public LgaInspectionItem PackageSize { get; private set; } = LgaInspectionItem.PackageSize;
    }

    public class QfnPackageProvider : IPackageItemProvider<QfnInspectionItem>
    {
        public QfnInspectionItem NoDevice { get; private set; } = QfnInspectionItem.NoDevice;

        public QfnInspectionItem PackageOffset { get; private set; } = QfnInspectionItem.PackageOffset;

        public QfnInspectionItem PackageSize { get; private set; } = QfnInspectionItem.PackageSize;
    }
}
