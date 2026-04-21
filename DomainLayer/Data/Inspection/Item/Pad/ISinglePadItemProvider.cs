namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Pad
{
    public interface ISinglePadItemProvider<T> where T : IInspectionItem
    {
        public T PadSize { get; }

        public T PadArea { get; }
    }

    public class QfnSinglePadItemProvider : ISinglePadItemProvider<QfnInspectionItem>
    {
        public QfnInspectionItem PadSize { get; private set; } = QfnInspectionItem.PadSize;
        public QfnInspectionItem PadArea { get; private set; } = QfnInspectionItem.PadArea;
    }
}
