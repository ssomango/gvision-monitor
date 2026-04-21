namespace GVisionWpf.Models.UiModels
{
    public class History
    {
        public int Id { get; set; }

        public DateTime Time { get; set; } = DateTime.Now;

        public string? Package { get; set; }

        public string? LotNumber { get; set; }

        public ECamera? Camera { get; set; }

        public EInspection? Inspection { get; set; }

        public ELog? LogType { get; set; }

        public string? Description { get; set; }

        public string? ImagePath { get; set; }

        public History() { }
    }
}
