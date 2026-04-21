namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Ball
{
    public interface IBallItemProvider<T> where T : IInspectionItem
    {
        public T BallCount { get; }

        public T BallSize { get; }

        public T BallPitch { get; }

        public T BallBridging { get; }

        public T ExtraBall { get; }

        public T MissingBall { get; }

        public T CrackBall { get; }

        public T BallPosition { get; }

        public T BallLight { get; }
    }

    public class BgaBallItemProvider : IBallItemProvider<BgaInspectionItem>
    {
        public BgaInspectionItem BallCount { get; private set; } = BgaInspectionItem.BallCount;

        public BgaInspectionItem BallSize { get; private set; } = BgaInspectionItem.BallSize;

        public BgaInspectionItem BallPitch { get; private set; } = BgaInspectionItem.BallPitch;

        public BgaInspectionItem BallBridging { get; private set; } = BgaInspectionItem.BallBridging;

        public BgaInspectionItem ExtraBall { get; private set; } = BgaInspectionItem.ExtraBall;

        public BgaInspectionItem MissingBall { get; private set; } = BgaInspectionItem.MissingBall;

        public BgaInspectionItem CrackBall { get; private set; } = BgaInspectionItem.CrackBall;

        public BgaInspectionItem BallPosition { get; private set; } = BgaInspectionItem.BallPosition;

        public BgaInspectionItem BallLight { get; private set; } = BgaInspectionItem.BallLight;
    }
}
