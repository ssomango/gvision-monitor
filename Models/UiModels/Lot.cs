namespace GVisionWpf.Models.UiModels
{
    public class Lot
    {
        public int Id { get; set; }
        public string Package { get; set; }
        public string LotNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Ellapsed { get; set; }

        public Lot() { }

        public Lot(int id, string package, string lotNumber)
        {
            Id = id;
            Package = package;
            LotNumber = lotNumber;
        }
    }
}
