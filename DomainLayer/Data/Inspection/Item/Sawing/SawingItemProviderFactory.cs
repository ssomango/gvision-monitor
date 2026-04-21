namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing
{
    public class SawingItemProviderFactory
    {
        public static ISawingItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(MoldInspectionItem)) => (ISawingItemProvider<TItem>)new MapSawingItemProvider(),
                Type t when t.Equals(typeof(BgaInspectionItem)) => (ISawingItemProvider<TItem>)new BgaSawingItemProvider(),
                Type t when t.Equals(typeof(LgaInspectionItem)) => (ISawingItemProvider<TItem>)new LgaSawingItemProvider(),
                Type t when t.Equals(typeof(QfnInspectionItem)) => (ISawingItemProvider<TItem>)new QfnSawingItemProvider(),
                _ => null
            };
        }
    }
}
