namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Package
{
    public interface IPackageInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T NoDevice { get; }

        public static abstract T PackageOffset { get; }

        public static abstract T PackageSize { get; }
    }
}
