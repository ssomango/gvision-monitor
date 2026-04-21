namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Package
{
    public class PackageItemProviderFactory
    {
        public static IPackageItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            return typeof(TItem) switch
            {
                Type t when t.Equals(typeof(BgaInspectionItem)) => (IPackageItemProvider<TItem>)new BgaPackageProvider(),
                Type t when t.Equals(typeof(MoldInspectionItem)) => (IPackageItemProvider<TItem>)new MapPackageProvider(),
                Type t when t.Equals(typeof(LgaInspectionItem)) => (IPackageItemProvider<TItem>)new LgaPackageProvider(),
                Type t when t.Equals(typeof(QfnInspectionItem)) => (IPackageItemProvider<TItem>)new QfnPackageProvider(),
                _ => null
            };
        }
    }
}
