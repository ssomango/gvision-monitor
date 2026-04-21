namespace GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark
{
    public interface IRejectMarkItemProvider<T> where T : IInspectionItem
    {
        public T RejectMark { get; }
    }

    public class MapRejectMarkProvider : IRejectMarkItemProvider<MoldInspectionItem>
    {
        public MoldInspectionItem RejectMark { get; private set; } = MoldInspectionItem.RejectMark;
    }

    public class BgaRejectMarkProvider : IRejectMarkItemProvider<BgaInspectionItem>
    {
        public BgaInspectionItem RejectMark { get; private set; } = BgaInspectionItem.RejectMark;
    }

    public class LgaRejectMarkProvider : IRejectMarkItemProvider<LgaInspectionItem>
    {
        public LgaInspectionItem RejectMark { get; private set; } = LgaInspectionItem.RejectMark;
    }

    public class QfnRejectMarkProvider : IRejectMarkItemProvider<QfnInspectionItem>
    {
        public QfnInspectionItem RejectMark { get; private set; } = QfnInspectionItem.RejectMark;
    }
}
