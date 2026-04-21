namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Lead
{
    public interface ILeadInspectionItem<T> :
        ILeadCountInspectionItem<T>,
        ILeadSizeInspectionItem<T>,
        ILeadAreaInspectionItem<T>,
        ILeadPitchInspectionItem<T>,
        ILeadOffsetInspectionItem<T>,
        ILeadContaminationInspectionItem<T>,
        ILeadPerimeterInspectionItem<T> where T : IInspectionItem
    {

    }

    public interface ILeadCountInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T LeadCount { get; }
    }

    public interface ILeadSizeInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T LeadSize { get; }
    }

    public interface ILeadAreaInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T LeadArea { get; }
    }

    public interface ILeadPitchInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T LeadPitch { get; }
    }

    public interface ILeadOffsetInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T LeadOffset { get; }
    }

    public interface ILeadContaminationInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T LeadContamination { get; }
    }

    public interface ILeadPerimeterInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T LeadPerimeter { get; }
    }
}
