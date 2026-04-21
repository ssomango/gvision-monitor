using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.MultiPad
{
    public interface IMultiPadContaminationResultModel<T> where T : InspectionResult
    {
        public Result<int> MultiPadContamination { get; set; }

        public IMultiPadContaminationResultModel<T> MergeTo(IMultiPadContaminationResultModel<T> model)
        {
            model.MultiPadContamination = MultiPadContamination;
            return model;
        }
    }
}
