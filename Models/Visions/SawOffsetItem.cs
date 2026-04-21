using System.Collections.ObjectModel;

namespace GVisionWpf.Models.Visions
{
    public class SawOffsetItem
    {


        public Point SawOffsetTargetPoint { get; set; } = new Point(10, 10);
        public ESawOffsetStandardObject SelectedSawOffsetStandardObject { get; set; }
        public HashSet<EDirection> Directions { get; set; } = new HashSet<EDirection>();
        public Dictionary<EDirection, double> TaughtDistances { get; set; } = new Dictionary<EDirection, double>();

        public ICollection<ESawOffsetStandardObject> SawOffsetStandardObjects { get; } = new ObservableCollection<ESawOffsetStandardObject>();
       
        public SawOffsetItem() { }

        public SawOffsetItem(ICollection<ESawOffsetStandardObject> sources)
        {
            SawOffsetStandardObjects = sources;
        }
    }
}
