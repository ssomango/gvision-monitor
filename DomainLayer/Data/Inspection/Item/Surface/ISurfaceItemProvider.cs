namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Surface
{
    public interface ISurfaceItemProvider<T> where T : IInspectionItem
    {
        public T Scratch { get; }
        public T ForeignMaterial { get; }
        public T Contamination { get; }
    }

    public class MapSurfaceItemProvider : ISurfaceItemProvider<MoldInspectionItem>
    {
        public MoldInspectionItem Scratch { get; private set; } = MoldInspectionItem.Scratch;

        public MoldInspectionItem ForeignMaterial { get; private set; } = MoldInspectionItem.ForeignMaterial;

        public MoldInspectionItem Contamination { get; private set; } = MoldInspectionItem.Contamination;
    }

    public class BgaSurfaceItemProvider : ISurfaceItemProvider<BgaInspectionItem>
    {
        public BgaInspectionItem Scratch { get; private set; } = BgaInspectionItem.Scratch;

        public BgaInspectionItem ForeignMaterial { get; private set; } = BgaInspectionItem.ForeignMaterial;

        public BgaInspectionItem Contamination { get; private set; } = BgaInspectionItem.Contamination;
    }

    public class LgaSurfaceItemProvider : ISurfaceItemProvider<LgaInspectionItem>
    {
        public LgaInspectionItem Scratch { get; private set; } = LgaInspectionItem.Scratch;

        public LgaInspectionItem ForeignMaterial { get; private set; } = LgaInspectionItem.ForeignMaterial;

        public LgaInspectionItem Contamination { get; private set; } = LgaInspectionItem.Contamination;
    }

    public class QfnSurfaceItemProvider : ISurfaceItemProvider<QfnInspectionItem>
    {
        public QfnInspectionItem Scratch { get; private set; } = QfnInspectionItem.Scratch;

        public QfnInspectionItem ForeignMaterial { get; private set; } = QfnInspectionItem.ForeignMaterial;

        public QfnInspectionItem Contamination { get; private set; } = QfnInspectionItem.Contamination;
    }
}
