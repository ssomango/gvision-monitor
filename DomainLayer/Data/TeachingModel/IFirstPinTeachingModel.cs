namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IFirstPinTeachingModel<T> where T : InspectionTeaching
    {
        public EFirstPin FirstPinType { get; set; }

        public Roi FirstPinRoi { get; set; }

        public Threshold FirstPinThreshold { get; set; }

        public Rect FirstPinRect { get; set; }

        public IFirstPinTeachingModel<T> MeregTo(IFirstPinTeachingModel<T> model)
        {
            model.FirstPinType = FirstPinType;
            model.FirstPinThreshold = FirstPinThreshold;
            return model;
        }
    }
}
