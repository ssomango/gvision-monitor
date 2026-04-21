namespace GVisionWpf.DomainLayer.Data.Inspection.Item.Ball
{
    public interface IBallInspectionItem<T> where T : IInspectionItem
    {
        public static abstract T BallCount { get; }

        public static abstract T BallSize { get; }

        public static abstract T BallPitch { get; }

        public static abstract T BallBridging { get; }

        public static abstract T ExtraBall { get; }

        public static abstract T MissingBall { get; }

        public static abstract T CrackBall { get; }

        public static abstract T BallPosition { get; }
    }
}
