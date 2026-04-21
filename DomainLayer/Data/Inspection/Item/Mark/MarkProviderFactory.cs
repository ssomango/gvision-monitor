namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Mark
{
    public class MarkProviderFactory
    {
        public static IMarkItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(MoldInspectionItem)) => (IMarkItemProvider<TItem>)new MapMarkProvider(),
                _ => null
            };
        }
    }
}
