namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Lead
{
    public static class LeadItemProviderFactory
    {
        public static ILeadItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            Type type = typeof(TItem);

            return type switch
            {
                Type t when t.Equals(typeof(LgaInspectionItem)) => (ILeadItemProvider<TItem>)new LgaLeadItemProvider(),
                Type t when t.Equals(typeof(QfnInspectionItem)) => (ILeadItemProvider<TItem>)new QfnLeadItemProvider(),
                _ => null
            };
        }
    }
}
