namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing
{
    public interface ISawingItemProvider<T> where T : IInspectionItem
    {
        public T CornerDegree { get; }

        public T SawOffset { get; }

        public T Chipping { get; }

        public T Burr { get; }
    }

    public class MapSawingItemProvider : ISawingItemProvider<MoldInspectionItem>
    {
        public MoldInspectionItem CornerDegree { get; private set; } = MoldInspectionItem.CornerDegree;

        public MoldInspectionItem SawOffset { get; private set; } = MoldInspectionItem.SawOffset;

        public MoldInspectionItem Chipping { get; private set; } = MoldInspectionItem.CornerDegree;

        public MoldInspectionItem Burr { get; private set; } = MoldInspectionItem.Burr;
    }

    public class BgaSawingItemProvider : ISawingItemProvider<BgaInspectionItem>
    {
        public BgaInspectionItem CornerDegree { get; private set; } = BgaInspectionItem.CornerDegree;

        public BgaInspectionItem SawOffset { get; private set; } = BgaInspectionItem.SawOffset;

        public BgaInspectionItem Chipping { get; private set; } = BgaInspectionItem.Chipping;

        public BgaInspectionItem Burr { get; private set; } = BgaInspectionItem.Burr;
    }

    public class LgaSawingItemProvider : ISawingItemProvider<LgaInspectionItem>
    {
        public LgaInspectionItem CornerDegree { get; private set; } = LgaInspectionItem.CornerDegree;

        public LgaInspectionItem SawOffset { get; private set; } = LgaInspectionItem.SawOffset;

        public LgaInspectionItem Chipping { get; private set; } = LgaInspectionItem.Chipping;

        public LgaInspectionItem Burr { get; private set; } = LgaInspectionItem.Burr;
    }

    public class QfnSawingItemProvider : ISawingItemProvider<QfnInspectionItem>
    {
        public QfnInspectionItem CornerDegree { get; private set; } = QfnInspectionItem.CornerDegree;

        public QfnInspectionItem SawOffset { get; private set; } = QfnInspectionItem.SawOffset;

        public QfnInspectionItem Chipping { get; private set; } = QfnInspectionItem.Chipping;

        public QfnInspectionItem Burr { get; private set; } = QfnInspectionItem.Burr;
    }
}
