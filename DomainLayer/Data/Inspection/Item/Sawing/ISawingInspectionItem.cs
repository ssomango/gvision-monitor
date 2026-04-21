namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing
{
    public interface ISawingInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T CornerDegree { get; }

        public static abstract T SawOffset { get; }

        public static abstract T Chipping { get; }

        public static abstract T Burr { get; }
    }
}
