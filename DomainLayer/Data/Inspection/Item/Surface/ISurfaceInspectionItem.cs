namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Surface
{
    public interface ISurfaceInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T Scratch { get; }
        public static abstract T ForeignMaterial { get; }
        public static abstract T Contamination { get; }
    }
}
