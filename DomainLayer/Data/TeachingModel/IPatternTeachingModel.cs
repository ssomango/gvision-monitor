using System.Collections.ObjectModel;

namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IPatternTeachingModel<T> where T : InspectionTeaching
    {
        ObservableCollection<Roi> PatternRois { get; set; }

        Threshold PatternThreshold { get; set; }

        List<Rect> Patterns { get; set; }

        public IPatternTeachingModel<T> MergeTo(IPatternTeachingModel<T> model)
        {
            model.PatternThreshold = PatternThreshold;
            model.Patterns = Patterns;
            return model;
        }
    }
}
