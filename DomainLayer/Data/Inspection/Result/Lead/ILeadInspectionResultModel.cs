using GVisionWpf.DomainLayer.Data.Inspection.Result;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Lead;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Interfaces.Inspect.Lead
{
    public partial interface ILeadInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> LeadCount { get; set; }

        public Result<List<LengthStatsInRoi>> LeadPitch { get; set; }

        public Result<StatisticalList<Pose>> LeadOffset { get; set; }

        public Result<StatisticalList<Size>> LeadSize { get; set; }

        public Result<StatisticalList<Length>> LeadPerimeter { get; set; }

        public Result<StatisticalList<Ratio>> LeadArea { get; set; }


        public ILeadInspectionResultModel<T> MergeTo(ILeadInspectionResultModel<T> model)
        {
            model.LeadCount = LeadCount;
            model.LeadPitch = LeadPitch;
            model.LeadOffset = LeadOffset;
            model.LeadSize = LeadSize;
            model.LeadPerimeter = LeadPerimeter;
            model.LeadArea = LeadArea;
            model.LeadContamination = LeadContamination;
            return model;
        }
    }

    partial interface ILeadInspectionResultModel<T> : ILeadContaminationResultModel<T> { }
}
