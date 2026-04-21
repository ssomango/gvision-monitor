namespace GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark
{
    public class RejectMarkItemProviderFactory
    {
        public static IRejectMarkItemProvider<TItem>? GetProvider<TItem>() where TItem : IInspectionItem
        {
            return typeof(TItem) switch
            {
                Type t when t.Equals(typeof(MoldInspectionItem)) => (IRejectMarkItemProvider<TItem>)new MapRejectMarkProvider(),
                Type t when t.Equals(typeof(BgaInspectionItem)) => (IRejectMarkItemProvider<TItem>)new BgaRejectMarkProvider(),
                Type t when t.Equals(typeof(LgaInspectionItem)) => (IRejectMarkItemProvider<TItem>)new LgaRejectMarkProvider(),
                Type t when t.Equals(typeof(QfnInspectionItem)) => (IRejectMarkItemProvider<TItem>)new QfnRejectMarkProvider(),
                _ => null
            };
        }
    }
}
