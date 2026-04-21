namespace GVisionWpf.DomainLayer.Data.Inspection.Item.MultiPad
{
    public static class MultiPadItemProviderFactory
    {
        public static IMultiPadItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(LgaInspectionItem)) => (IMultiPadItemProvider<TItem>)new LgaMultiPadItemProvider(),
                _ => null
            };
        }
    }
}
