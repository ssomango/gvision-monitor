namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Ball
{
    public interface IBallInspectionResultModel<T> : IDisposable where T : IInspectionResultModel
    {
        public Result<int> BallCount { get; set; }
        public Result<StatisticalList<Length>> BallSize { get; set; }
        public Result<StatisticalList<Length>> BallPitch { get; set; }
        public Result<int> BallBridging { get; set; }
        public Result<int> ExtraBall { get; set; }
        public Result<int> MissingBall { get; set; }
        public Result<int> CrackBall { get; set; }
        public Result<int> BallPosition { get; set; }
        public Result<int> BallLight { get; set; }

        public IBallInspectionResultModel<T> MergeTo(IBallInspectionResultModel<T> model)
        {
            model.BallCount = BallCount;
            model.BallSize = BallSize;
            model.BallPitch = BallPitch;
            model.BallBridging = BallBridging;
            model.ExtraBall = ExtraBall;
            model.MissingBall = MissingBall;
            model.CrackBall = CrackBall;
            model.BallPosition = BallPosition;
            model.BallLight = BallLight;


            return model;
        }
    }
}
