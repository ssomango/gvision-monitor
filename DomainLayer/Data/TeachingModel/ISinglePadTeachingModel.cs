namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface ISinglePadTeachingModel<T> where T : InspectionTeaching
    {
        public Roi PadRoi { get; set; }

        public Threshold PadThreshold { get; set; }

        public Size PadSize { get; set; }

        public int PadArea { get; set; }

        public ISinglePadTeachingModel<T> MergeTo(ISinglePadTeachingModel<T> model)
        {
            model.PadRoi.CopyFrom(this.PadRoi);
            model.PadThreshold = PadThreshold;
            model.PadSize = PadSize;
            model.PadArea = PadArea;
            return model;
        }
    }
}
