using System.Collections.ObjectModel;

namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IForeignMaterialTeachingModel<T> where T : InspectionTeaching
    {
        Threshold ForeignMaterialThreshold { get; set; }

        int ForeignMaterialMinSize { get; set; }

        int ForeignMaterialMaxSize { get; set; }

        ObservableCollection<Roi> SurfaceRois { get; set; }

        public IForeignMaterialTeachingModel<T> MergeTo(IForeignMaterialTeachingModel<T> model)
        {
            model.ForeignMaterialThreshold = ForeignMaterialThreshold;
            model.ForeignMaterialMinSize = ForeignMaterialMinSize;
            model.ForeignMaterialMaxSize = ForeignMaterialMaxSize;
            return model;
        }
    }
}
