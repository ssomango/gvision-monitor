using System.Collections.ObjectModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;

namespace GVisionWpf.Models.Entities.Recipe
{
    public partial class StripTeaching : InspectionTeaching { }

    partial class StripTeaching : IStripTeachingModel<StripTeaching>
    {
        public IEnumerable<Roi> StripRois { get; set; } = new ObservableCollection<Roi>();
    }
}
