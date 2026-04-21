namespace GVisionWpf.DomainLayer.Data.Inspection.Item.MultiPad
{
    public interface IMultiPadInspectionItem<T> :
        IMultiPadCountInspectionItem<T>,
        IMultiPadSizeInspectionItem<T>,
        IMultiPadAreaInspectionItem<T>,
        IMultiPadPitchInspectionItem<T>,
        IMultiPadOffsetInspectionItem<T>,
        IMultiPadOffsetContaminationInspectionItem<T>,
        IMultiPadPerimeterInspectionItem<T> where T : IInspectionItem
    {

    }

    public interface IMultiPadCountInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T MultiPadCount { get; }
    }

    public interface IMultiPadSizeInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T MultiPadSize { get; }
    }

    public interface IMultiPadAreaInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T MultiPadArea { get; }
    }

    public interface IMultiPadPitchInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T MultiPadPitch { get; }
    }

    public interface IMultiPadOffsetInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T MultiPadOffset { get; }
    }

    public interface IMultiPadOffsetContaminationInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T MultiPadContamination { get; }
    }

    public interface IMultiPadPerimeterInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T MultiPadPerimeter { get; }
    }
}
