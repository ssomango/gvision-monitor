namespace GVisionWpf.DomainLayer.Data.Inspection.Item.DataCode
{
    public interface IDataCodeInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T DataCode { get; }
    }
}
