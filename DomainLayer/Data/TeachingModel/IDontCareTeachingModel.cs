using System.Collections.ObjectModel;


namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IDontCareTeachingModel<T> where T : InspectionTeaching
    {
        ObservableCollection<Roi> DontCareRois { get; set; }

        public IDontCareTeachingModel<T> MergeTo(IDontCareTeachingModel<T> model)
        {
            return model;
        }
    }
}
