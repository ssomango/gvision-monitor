namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IMarkTeachingModel<T> where T : InspectionTeaching
    {
        Threshold MarkThreshold { get; set; }

        List<MarkItem> MarkItems { get; set; }

        Pose TextOffset { get; set; }

        public IMarkTeachingModel<T> MergeTo(IMarkTeachingModel<T> model)
        {
            model.MarkThreshold = MarkThreshold;
            model.MarkItems = MarkItems;
            model.TextOffset = TextOffset;
            return model;
        }
    }
}
