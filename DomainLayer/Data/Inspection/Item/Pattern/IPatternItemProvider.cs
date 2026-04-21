namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern
{
    public interface IPatternItemProvider<T> where T : IInspectionItem
    {
        public T Pattern { get; }
    }

    public class BgaPatternItemProvider : IPatternItemProvider<BgaInspectionItem>
    {
        public BgaInspectionItem Pattern { get; private set; } = BgaInspectionItem.Pattern;
    }
}
