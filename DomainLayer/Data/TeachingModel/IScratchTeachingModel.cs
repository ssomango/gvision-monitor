using System.Collections.ObjectModel;

namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IScratchTeachingModel<T> where T : InspectionTeaching
    {
        Threshold ScratchThreshold { get; set; }

        int ScratchMinSize { get; set; }

        int ScratchMaxSize { get; set; }

        ObservableCollection<Roi> SurfaceRois { get; set; }

        public IScratchTeachingModel<T> MergeTo(IScratchTeachingModel<T> model)
        {
            model.ScratchThreshold = ScratchThreshold;
            model.ScratchMinSize = ScratchMinSize;
            model.ScratchMaxSize = ScratchMaxSize;
            return model;
        }
    }
}
