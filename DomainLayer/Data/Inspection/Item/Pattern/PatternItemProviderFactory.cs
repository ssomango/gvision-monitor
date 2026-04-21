namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern
{
    public static class PatternItemProviderFactory
    {
        public static IPatternItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(BgaInspectionItem)) => (IPatternItemProvider<TItem>)new BgaPatternItemProvider(),
                _ => null
            };
        }
    }
}
