using GVisionWpf.DomainLayer.Data.Inspection.Result.DataCode;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Mark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Result.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Saw;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Surface;

namespace GVisionWpf.Models.Entities.Result
{
    public sealed partial class MapInspectionResult : InspectionResult
    {
        //public List<HObject> Shots = new List<HObject>();

    }

    partial class MapInspectionResult : IPackageInspectionResultModel<MapInspectionResult>
    {

    }

    partial class MapInspectionResult : IRejectMarkInspectionResultModel<MapInspectionResult>
    {
        public Result<int> RejectMark { get; set; } = new Result<int>();
    }

    partial class MapInspectionResult : IDataCodeInspectionResultModel<MapInspectionResult>
    {
        public Result<string> DataCode { get; set; } = new Result<string>();
    }

    partial class MapInspectionResult : IMarkInspectionResultModel<MapInspectionResult>
    {
        public Result<int> MissingCharacter { get; set; } = new Result<int>();
        public Result<int> NoMark { get; set; } = new Result<int>();
        public Result<string> Mark { get; set; } = new Result<string>();
        public Result<Pose> TextOffset { get; set; } = new Result<Pose>();
    }

    partial class MapInspectionResult : IScratchInspectionResultModel<MapInspectionResult>
    {
        public Result<int> Scratch { get; set; } = new Result<int>();
    }

    partial class MapInspectionResult : IForeignMaterialInspectionResultModel<MapInspectionResult>
    {
        public Result<int> ForeignMaterial { get; set; } = new Result<int>();
    }

    partial class MapInspectionResult : IContaminationInspectionResultModel<MapInspectionResult>
    {
        public Result<int> Contamination { get; set; } = new Result<int>();
    }

    partial class MapInspectionResult : ICornerDegreeInspectionResultModel<MapInspectionResult>
    {
        public Result<CornerDegree> CornerDegree { get; set; } = new Result<CornerDegree>();
    }

    partial class MapInspectionResult : IChippingInspectionResultModel<MapInspectionResult>
    {
        public Result<int> Chipping { get; set; } = new Result<int>();
    }

    partial class MapInspectionResult : ISawOffsetInspectionResultModel<MapInspectionResult>
    {
        public Result<SawOffset> SawOffset { get; set; } = new Result<SawOffset>();
    }
}