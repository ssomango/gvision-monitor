namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Mark
{
    public interface IMarkItemProvider<T> where T : IInspectionItem
    {
        public T NoMark { get; }

        public T TextAngle { get; }

        public T TextOffset { get; }

        public T MissingChar { get; }

        public T MarkCount { get; }

        public T WrongMark { get; }
    }

    public class MapMarkProvider : IMarkItemProvider<MoldInspectionItem>
    {
        public MoldInspectionItem NoMark { get; private set; } = MoldInspectionItem.NoMark;

        public MoldInspectionItem TextAngle { get; private set; } = MoldInspectionItem.TextAngle;

        public MoldInspectionItem TextOffset { get; private set; } = MoldInspectionItem.TextOffset;

        public MoldInspectionItem MissingChar { get; private set; } = MoldInspectionItem.MissingChar;

        public MoldInspectionItem MarkCount { get; private set; } = MoldInspectionItem.MarkCount;

        public MoldInspectionItem WrongMark { get; private set; } = MoldInspectionItem.WrongMark;
    }
}
