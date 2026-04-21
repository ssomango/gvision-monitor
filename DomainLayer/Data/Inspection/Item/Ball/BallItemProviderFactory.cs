namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Ball
{
    public static class BallItemProviderFactory
    {
        public static IBallItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(BgaInspectionItem)) => (IBallItemProvider<TItem>)new BgaBallItemProvider(),
                _ => null
            };
        }
    }
}
