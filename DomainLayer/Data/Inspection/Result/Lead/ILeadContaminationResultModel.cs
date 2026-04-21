using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Lead
{
    public interface ILeadContaminationResultModel<T> where T : InspectionResult
    {
        public Result<int> LeadContamination { get; set; }

        public ILeadContaminationResultModel<T> MergeTo(ILeadContaminationResultModel<T> model)
        {
            model.LeadContamination = LeadContamination;
            return model;
        }
    }
}
