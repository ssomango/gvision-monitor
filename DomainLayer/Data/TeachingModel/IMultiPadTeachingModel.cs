using System.Collections.ObjectModel;

namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IMultiPadTeachingModel<T> where T : InspectionTeaching
    {
        public ObservableCollection<Roi> PadRois { get; set; }

        public Threshold MultiPadThreshold { get; set; }

        public StatisticalList<Size> MultiPadSizes { get; set; }

        public Size MultiPadAvgSize { get; set; }

        public IEnumerable<Pose> PadPxPoses { get; set; }

        public int MultiPadAvgArea { get; set; }

        public double MultiPadAvgPitch { get; set; }

        public double MultiPadAvgPerimeter { get; set; }

        public int PadContaminationMinSize { get; set; }

        public int PadContaminationMaxSize { get; set; }

        public IMultiPadTeachingModel<T> MergeTo(IMultiPadTeachingModel<T> model)
        {
            //model.PadRois = this.PadRois;

            model.MultiPadThreshold = MultiPadThreshold;

            model.MultiPadSizes = MultiPadSizes;
            model.MultiPadAvgSize = MultiPadAvgSize;

            model.PadPxPoses = PadPxPoses;

            model.MultiPadAvgArea = MultiPadAvgArea;
            model.MultiPadAvgPitch = MultiPadAvgPitch;
            model.MultiPadAvgPerimeter = MultiPadAvgPerimeter;

            model.PadContaminationMinSize = PadContaminationMinSize;
            model.PadContaminationMaxSize = PadContaminationMaxSize;
            return model;
        }
    }
}
