namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Pad
{
    public static class SinglePadItemProviderFactory
    {
        public static ISinglePadItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(QfnInspectionItem)) => (ISinglePadItemProvider<TItem>)new QfnSinglePadItemProvider(),
                _ => null
            };
        }
    }
}
