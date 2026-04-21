namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Pad
{
    public interface ISinglePadInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T PadSize { get; }
        public static abstract T PadArea { get; }
    }
}
