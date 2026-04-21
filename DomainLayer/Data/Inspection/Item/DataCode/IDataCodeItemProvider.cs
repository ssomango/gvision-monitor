namespace GVisionWpf.DomainLayer.Data.Inspection.Item.DataCode
{
    public interface IDataCodeItemProvider<T> where T : IInspectionItem
    {
        public T DataCode { get; }
    }

    public class MapDataCodeItemProvider : IDataCodeItemProvider<MoldInspectionItem>
    {
        public MoldInspectionItem DataCode { get; private set; } = MoldInspectionItem.DataCode;
    }
}
