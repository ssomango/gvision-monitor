namespace GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin
{
    public static class FirstPinItemProviderFactory
    {
        public static IFirstPinItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(BgaInspectionItem)) => (IFirstPinItemProvider<TItem>)new BgaFirstPinItemProvider(),
                Type t when t.Equals(typeof(LgaInspectionItem)) => (IFirstPinItemProvider<TItem>)new LgaFirstPinItemProvider(),
                Type t when t.Equals(typeof(QfnInspectionItem)) => (IFirstPinItemProvider<TItem>)new QfnFirstPinItemProvider(),
                _ => null
            };
        }
    }
}

