namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IDataCodeTeachingModel<T> where T : InspectionTeaching
    {
        Roi CodeRoi { get; set; }

        public IDataCodeTeachingModel<T> MergeTo(IDataCodeTeachingModel<T> model)
        {
            model.CodeRoi = CodeRoi;
            return model;
        }
    }
}
