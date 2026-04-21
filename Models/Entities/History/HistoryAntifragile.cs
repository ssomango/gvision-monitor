namespace GVisionWpf.Models.Entities.History
{
    public class HistoryAntifragile
    {
        public int Id { get; set; }

        public DateTime Time { get; set; } = DateTime.Now;

        public String? Package { get; set; }

        public int? LotId { get; set; }

        public ECamera? Camera { get; set; }

        public EInspection? Inspection { get; set; }

        public ELog? LogType { get; set; }

        public String? Description { get; set; }

        public String? ImagePath { get; set; }

        public HistoryAntifragile() { }
    }
}
