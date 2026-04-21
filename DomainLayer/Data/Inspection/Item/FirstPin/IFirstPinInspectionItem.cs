namespace GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin
{
    public interface IFirstPinInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T FirstPin { get; }
    }
}
