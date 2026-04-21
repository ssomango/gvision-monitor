namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IStripTeachingModel<T> where T : InspectionTeaching
    {
        public IEnumerable<Roi> StripRois { get; set; }
    }
}
