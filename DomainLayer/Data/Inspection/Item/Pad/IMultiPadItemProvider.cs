namespace GVisionWpf.DomainLayer.Data.Inspection.Item.MultiPad
{
    public interface IMultiPadItemProvider<T> where T : IInspectionItem
    {
        public T MultiPadCount { get; }
        public T MultiPadSize { get; }
        public T MultiPadArea { get; }
        public T MultiPadPitch { get; }
        public T MultiPadOffset { get; }
        public T MultiPadContamination { get; }
        public T MultiPadPerimeter { get; }
    }

    public class LgaMultiPadItemProvider : IMultiPadItemProvider<LgaInspectionItem>
    {
        public LgaInspectionItem MultiPadCount { get; private set; } = LgaInspectionItem.MultiPadCount;

        public LgaInspectionItem MultiPadSize { get; private set; } = LgaInspectionItem.MultiPadSize;

        public LgaInspectionItem MultiPadArea { get; private set; } = LgaInspectionItem.MultiPadArea;

        public LgaInspectionItem MultiPadPitch { get; private set; } = LgaInspectionItem.MultiPadPitch;

        public LgaInspectionItem MultiPadOffset { get; private set; } = LgaInspectionItem.MultiPadOffset;

        public LgaInspectionItem MultiPadContamination { get; private set; } = LgaInspectionItem.MultiPadContamination;

        public LgaInspectionItem MultiPadPerimeter { get; private set; } = LgaInspectionItem.MultiPadPerimeter;
    }
}
