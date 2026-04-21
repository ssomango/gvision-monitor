namespace GVisionWpf.Models.Entities.Lot
{
    public class LotAntifragile
    {
        public int Id { get; set; }
        public String? Package { get; set; }
        public String? LotNumber { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public LotAntifragile() { }

        public static implicit operator List<object>(LotAntifragile? v)
        {
            throw new NotImplementedException();
        }
    }
}
