using System.Collections.ObjectModel;


namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IContaminationTeachingModel<T> where T : InspectionTeaching
    {
        Threshold ContaminationThreshold { get; set; }

        int ContaminationMinSize { get; set; }

        int ContaminationMaxSize { get; set; }

        ObservableCollection<Roi> SurfaceRois { get; set; }

        public IContaminationTeachingModel<T> MergeTo(IContaminationTeachingModel<T> model)
        {
            model.ContaminationThreshold = ContaminationThreshold;
            model.ContaminationMinSize = ContaminationMinSize;
            model.ContaminationMaxSize = ContaminationMaxSize;
            return model;
        }
    }
}
