namespace GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin
{
    public interface IFirstPinItemProvider<T> where T : IInspectionItem
    {
        public T FirstPin { get; }
    }

    public class BgaFirstPinItemProvider : IFirstPinItemProvider<BgaInspectionItem>
    {
        public BgaInspectionItem FirstPin { get; private set; } = BgaInspectionItem.FirstPin;
    }

    public class LgaFirstPinItemProvider : IFirstPinItemProvider<LgaInspectionItem>
    {
        public LgaInspectionItem FirstPin { get; private set; } = LgaInspectionItem.FirstPin;
    }

    public class QfnFirstPinItemProvider : IFirstPinItemProvider<QfnInspectionItem>
    {
        public QfnInspectionItem FirstPin { get; private set; } = QfnInspectionItem.FirstPin;
    }
}
