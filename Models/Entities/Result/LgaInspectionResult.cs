using GVisionWpf.DomainLayer.Data.Inspection.Result.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Result.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;
using GVisionWpf.Interfaces.Inspect.Lead;
using GVisionWpf.Interfaces.MultiPad;

namespace GVisionWpf.Models.Entities.Result
{
    public sealed partial class LgaInspectionResult : InspectionResult
    {

    }
    partial class LgaInspectionResult : IPackageInspectionResultModel<LgaInspectionResult>
    {

    }

    partial class LgaInspectionResult : IFirstPinInspectResultModel<LgaInspectionResult>
    {
        public Result<bool> FirstPin { get; set; } = new Result<bool>();
    }

    partial class LgaInspectionResult : IRejectMarkInspectionResultModel<LgaInspectionResult>
    {
        public Result<int> RejectMark { get; set; } = new Result<int>();
    }

    partial class LgaInspectionResult : IMultiPadInspectionResultModel<LgaInspectionResult>
    {
        public Result<List<LengthStatsInRoi>> MultiPadPitch { get; set; } = new Result<List<LengthStatsInRoi>>();

        public Result<StatisticalList<Length>> MultiPadPerimeter { get; set; } = new Result<StatisticalList<Length>>();

        public Result<int> MultiPadCount { get; set; } = new Result<int>();

        public Result<StatisticalList<Pose>> MultiPadOffset { get; set; } = new Result<StatisticalList<Pose>>();

        public Result<int> MultiPadContamination { get; set; } = new Result<int>();

        public Result<StatisticalList<Size>> MultiPadSize { get; set; } = new Result<StatisticalList<Size>>();

        public Result<StatisticalList<Ratio>> MultiPadArea { get; set; } = new Result<StatisticalList<Ratio>>();
    }

    partial class LgaInspectionResult : ILeadInspectionResultModel<LgaInspectionResult>
    {
        public Result<int> LeadCount { get; set; } = new Result<int>();

        public Result<List<LengthStatsInRoi>> LeadPitch { get; set; } = new Result<List<LengthStatsInRoi>>();

        public Result<StatisticalList<Pose>> LeadOffset { get; set; } = new Result<StatisticalList<Pose>>();

        public Result<StatisticalList<Size>> LeadSize { get; set; } = new Result<StatisticalList<Size>>();

        public Result<StatisticalList<Length>> LeadPerimeter { get; set; } = new Result<StatisticalList<Length>>();

        public Result<StatisticalList<Ratio>> LeadArea { get; set; } = new Result<StatisticalList<Ratio>>();

        public Result<int> LeadContamination { get; set; } = new Result<int>();
    }

    partial class LgaInspectionResult : ICornerDegreeInspectionResultModel<LgaInspectionResult>
    {
        public Result<CornerDegree> CornerDegree { get; set; } = new Result<CornerDegree>();
    }

    partial class LgaInspectionResult : IChippingInspectionResultModel<LgaInspectionResult>
    {
        public Result<int> Chipping { get; set; } = new Result<int>();
    }

    partial class LgaInspectionResult : IBurrInspectionResultModel<LgaInspectionResult>
    {
        public Result<int> Burr { get; set; } = new Result<int>();
    }

    partial class LgaInspectionResult : ISawOffsetInspectionResultModel<LgaInspectionResult>
    {
        public Result<SawOffset> SawOffset { get; set; } = new Result<SawOffset>();
    }

    partial class LgaInspectionResult : IScratchInspectionResultModel<LgaInspectionResult>
    {
        public Result<int> Scratch { get; set; } = new Result<int>();
    }

    partial class LgaInspectionResult : IForeignMaterialInspectionResultModel<LgaInspectionResult>
    {
        public Result<int> ForeignMaterial { get; set; } = new Result<int>();
    }

    partial class LgaInspectionResult : IContaminationInspectionResultModel<LgaInspectionResult>
    {
        public Result<int> Contamination { get; set; } = new Result<int>();
    }

}