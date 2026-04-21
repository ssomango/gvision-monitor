namespace GVisionWpf.Models.Entities.Lot
{
    public class InspectionResultAntifragile
    {
        public int Id { get; set; }
        public int LotId { get; set; }
        public string RecipeName { get; set; }
        public long Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Item { get; set; }
        public int XPos { get; set; }
        public int YPos { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double TOffset { get; set; }
        public double PackageWidth { get; set; }
        public double PackageHeight { get; set; }
        public bool HasDevice { get; set; }
        public EInspection InspectionType { get; set; }

        public InspectionResultAntifragile() { }
    }
}
