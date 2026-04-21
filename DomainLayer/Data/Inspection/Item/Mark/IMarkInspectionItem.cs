namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Mark
{
    public interface IMarkInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T NoMark { get; }
        public static abstract T TextAngle { get; }
        public static abstract T TextOffset { get; }
        public static abstract T MissingChar { get; }
        public static abstract T MarkCount { get; }
        public static abstract T WrongMark { get; }

    }
}
