
using GVisionWpf.DomainLayer.Data.Inspection.Result.Ball;
using GVisionWpf.DomainLayer.Data.Inspection.Result.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Pattern;
using GVisionWpf.DomainLayer.Data.Inspection.Result.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;

namespace GVisionWpf.Models.Entities.Result
{
    public partial class BgaInspectionResult : InspectionResult { }

    public partial class BgaInspectionResult : IPackageInspectionResultModel<BgaInspectionResult> { }

    public partial class BgaInspectionResult : IFirstPinInspectResultModel<BgaInspectionResult>
    {
        public Result<bool> FirstPin { get; set; } = new Result<bool>();
    }

    public partial class BgaInspectionResult : IPatternInspectionResultModel<BgaInspectionResult>
    {
        public Result<int> PatternCount { get; set; } = new Result<int>();
    }

    public partial class BgaInspectionResult : IBallInspectionResultModel<BgaInspectionResult>
    {

        public Result<StatisticalList<Length>> BallSize { get; set; } = new Result<StatisticalList<Length>>();

        public Result<StatisticalList<Length>> BallPitch { get; set; } = new Result<StatisticalList<Length>>();

        public Result<int> BallBridging { get; set; } = new Result<int>();

        public Result<int> ExtraBall { get; set; } = new Result<int>();

        public Result<int> CrackBall { get; set; } = new Result<int>();

        public Result<int> BallLight { get; set; } = new Result<int>();

        public Result<int> BallPosition { get; set; } = new Result<int>();

        public Result<int> MissingBall { get; set; } = new Result<int>();

        public Result<int> BallCount { get; set; } = new Result<int>();

    }

    public partial class BgaInspectionResult :
        ISawOffsetInspectionResultModel<BgaInspectionResult>,
        IChippingInspectionResultModel<BgaInspectionResult>,
        IBurrInspectionResultModel<BgaInspectionResult>,
        ICornerDegreeInspectionResultModel<BgaInspectionResult>
    {
        public Result<CornerDegree> CornerDegree { get; set; } = new Result<CornerDegree>();
        public Result<int> Chipping { get; set; } = new Result<int>();
        public Result<int> Burr { get; set; } = new Result<int>();
    }

    public partial class BgaInspectionResult : IRejectMarkInspectionResultModel<BgaInspectionResult>
    {
        public Result<int> RejectMark { get; set; } = new Result<int>();
    }

    public partial class BgaInspectionResult :
        IScratchInspectionResultModel<BgaInspectionResult>,
        IForeignMaterialInspectionResultModel<BgaInspectionResult>,
        IContaminationInspectionResultModel<BgaInspectionResult>
    {
        public Result<int> Scratch { get; set; } = new Result<int>();
        public Result<int> ForeignMaterial { get; set; } = new Result<int>();
        public Result<int> Contamination { get; set; } = new Result<int>();
    }
}