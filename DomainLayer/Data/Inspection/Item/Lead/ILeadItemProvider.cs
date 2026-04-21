namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Lead
{
    public interface ILeadItemProvider<T> where T : IInspectionItem
    {
        public T LeadCount { get; }
        public T LeadSize { get; }
        public T LeadArea { get; }
        public T LeadPitch { get; }
        public T LeadOffset { get; }
        public T LeadContamination { get; }
        public T LeadPerimeter { get; }
    }

    public class LgaLeadItemProvider : ILeadItemProvider<LgaInspectionItem>
    {
        public LgaInspectionItem LeadCount { get; private set; } = LgaInspectionItem.LeadCount;

        public LgaInspectionItem LeadSize { get; private set; } = LgaInspectionItem.LeadSize;

        public LgaInspectionItem LeadArea { get; private set; } = LgaInspectionItem.LeadArea;

        public LgaInspectionItem LeadPitch { get; private set; } = LgaInspectionItem.LeadPitch;

        public LgaInspectionItem LeadOffset { get; private set; } = LgaInspectionItem.LeadOffset;

        public LgaInspectionItem LeadContamination { get; private set; } = LgaInspectionItem.LeadContamination;

        public LgaInspectionItem LeadPerimeter { get; private set; } = LgaInspectionItem.LeadPerimeter;
    }

    public class QfnLeadItemProvider : ILeadItemProvider<QfnInspectionItem>
    {
        public QfnInspectionItem LeadCount { get; private set; } = QfnInspectionItem.LeadCount;

        public QfnInspectionItem LeadSize { get; private set; } = QfnInspectionItem.LeadSize;

        public QfnInspectionItem LeadArea { get; private set; } = QfnInspectionItem.LeadArea;

        public QfnInspectionItem LeadPitch { get; private set; } = QfnInspectionItem.LeadPitch;

        public QfnInspectionItem LeadOffset { get; private set; } = QfnInspectionItem.LeadOffset;

        public QfnInspectionItem LeadContamination { get; private set; } = QfnInspectionItem.LeadContamination;

        public QfnInspectionItem LeadPerimeter { get; private set; } = QfnInspectionItem.LeadPerimeter;
    }
}
