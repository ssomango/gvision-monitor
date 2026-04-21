using System.Collections.ObjectModel;

namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface ILeadTeachingModel<T> where T : InspectionTeaching
    {
        ObservableCollection<Roi> LeadRois { get; set; }

        Threshold LeadThreshold { get; set; }

        IEnumerable<Pose> LeadPxPoses { get; set; }

        int LeadAverageArea { get; set; }

        double LeadAveragePerimeter { get; set; }

        double LeadAveragePitch { get; set; }

        Size LeadAverageSize { get; set; }

        int LeadContaminationMinSize { get; set; }

        int LeadContaminationMaxSize { get; set; }

        public ILeadTeachingModel<T> MergeTo(ILeadTeachingModel<T> model)
        {
            model.LeadThreshold = LeadThreshold;
            model.LeadPxPoses = LeadPxPoses;
            model.LeadAverageArea = LeadAverageArea;
            model.LeadAveragePerimeter = LeadAveragePerimeter;
            model.LeadAveragePitch = LeadAveragePitch;
            model.LeadAverageSize = LeadAverageSize;
            model.LeadContaminationMinSize = LeadContaminationMinSize;
            model.LeadContaminationMaxSize = LeadContaminationMaxSize;
            return model;
        }
    }
}
