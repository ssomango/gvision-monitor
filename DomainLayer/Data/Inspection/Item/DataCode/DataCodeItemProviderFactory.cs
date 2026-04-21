namespace GVisionWpf.DomainLayer.Data.Inspection.Item.DataCode
{
    public class DataCodeItemProviderFactory
    {
        public static IDataCodeItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(MoldInspectionItem)) => (IDataCodeItemProvider<TItem>)new MapDataCodeItemProvider(),
                _ => null
            };
        }
    }
}
