namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Surface
{
    internal class SurfaceItemProviderFactory
    {
        public static ISurfaceItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(MoldInspectionItem)) => (ISurfaceItemProvider<TItem>)new MapSurfaceItemProvider(),
                Type t when t.Equals(typeof(BgaInspectionItem)) => (ISurfaceItemProvider<TItem>)new BgaSurfaceItemProvider(),
                Type t when t.Equals(typeof(LgaInspectionItem)) => (ISurfaceItemProvider<TItem>)new LgaSurfaceItemProvider(),
                Type t when t.Equals(typeof(QfnInspectionItem)) => (ISurfaceItemProvider<TItem>)new QfnSurfaceItemProvider(),
                _ => null
            };
        }
    }
}
