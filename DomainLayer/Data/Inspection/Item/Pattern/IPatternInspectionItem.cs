namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern
{
    public interface IPatternInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T Pattern { get; }
    }
}
