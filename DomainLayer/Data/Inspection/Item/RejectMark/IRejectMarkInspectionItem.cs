namespace GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark
{
    public interface IRejectMarkInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T RejectMark { get; }
    }
}
