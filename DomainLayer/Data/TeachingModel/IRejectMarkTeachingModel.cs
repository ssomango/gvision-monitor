namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IRejectMarkTeachingModel<T> where T : InspectionTeaching
    {
        Roi RejectMarkRoi { get; set; }

        Threshold RejectMarkThreshold { get; set; }

        int RejectMarkMinSize { get; set; }

        int RejectMarkMaxSize { get; set; }

        public IRejectMarkTeachingModel<T> MergeTo(IRejectMarkTeachingModel<T> model)
        {
            model.RejectMarkThreshold = RejectMarkThreshold;
            model.RejectMarkMinSize = RejectMarkMinSize;
            model.RejectMarkMaxSize = RejectMarkMaxSize;
            return model;
        }
    }
}
