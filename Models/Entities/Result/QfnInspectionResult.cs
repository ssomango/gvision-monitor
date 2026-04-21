using GVisionWpf.DomainLayer.Data.Inspection.Result.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Pad;
using GVisionWpf.DomainLayer.Data.Inspection.Result.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;
using GVisionWpf.Interfaces.Inspect.Lead;

namespace GVisionWpf.Models.Entities.Result
{
    public partial class QfnInspectionResult : InspectionResult
    {
    }

    partial class QfnInspectionResult : IPackageInspectionResultModel<QfnInspectionResult> { }

    partial class QfnInspectionResult : IFirstPinInspectResultModel<QfnInspectionResult>
    {
        public Result<bool> FirstPin { get; set; } = new Result<bool>();
    }

    partial class QfnInspectionResult : ISinglePadInspectionResultModel<QfnInspectionResult>
    {
        public Result<Size> PadSize { get; set; } = new Result<Size>();
        public Result<Ratio> PadArea { get; set; } = new Result<Ratio>();
    }

    partial class QfnInspectionResult : ILeadInspectionResultModel<QfnInspectionResult>
    {
        public Result<int> LeadCount { get; set; } = new Result<int>();
        public Result<List<LengthStatsInRoi>> LeadPitch { get; set; } = new Result<List<LengthStatsInRoi>>();
        public Result<StatisticalList<Pose>> LeadOffset { get; set; } = new Result<StatisticalList<Pose>>();
        public Result<StatisticalList<Size>> LeadSize { get; set; } = new Result<StatisticalList<Size>>();
        public Result<StatisticalList<Length>> LeadPerimeter { get; set; } = new Result<StatisticalList<Length>>();
        public Result<StatisticalList<Ratio>> LeadArea { get; set; } = new Result<StatisticalList<Ratio>>();
        public Result<int> LeadContamination { get; set; } = new Result<int>();
    }

    partial class QfnInspectionResult :
        ICornerDegreeInspectionResultModel<QfnInspectionResult>,
        ISawOffsetInspectionResultModel<QfnInspectionResult>
    {
        public Result<CornerDegree> CornerDegree { get; set; } = new Result<CornerDegree>();

        public Result<SawOffset> SawOffset { get; set; } = new Result<SawOffset>();
    }

    partial class QfnInspectionResult :
        IChippingInspectionResultModel<QfnInspectionResult>,
        IBurrInspectionResultModel<QfnInspectionResult>
    {
        public Result<int> Chipping { get; set; } = new Result<int>();

        public Result<int> Burr { get; set; } = new Result<int>();
    }

    partial class QfnInspectionResult : IRejectMarkInspectionResultModel<QfnInspectionResult>
    {
        public Result<int> RejectMark { get; set; } = new Result<int>();
    }

    partial class QfnInspectionResult :
        IScratchInspectionResultModel<QfnInspectionResult>,
        IForeignMaterialInspectionResultModel<QfnInspectionResult>,
        IContaminationInspectionResultModel<QfnInspectionResult>
    {
        public Result<int> Scratch { get; set; } = new Result<int>();

        public Result<int> ForeignMaterial { get; set; } = new Result<int>();

        public Result<int> Contamination { get; set; } = new Result<int>();
    }
}