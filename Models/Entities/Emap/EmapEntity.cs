using System.ComponentModel.DataAnnotations.Schema;

namespace GVisionWpf.Models.Entities.Emap
{
    public class EmapEntity
    {
        public int Id { get; set; }
        public int LotId { get; set; }
        public int? XPickPosition { get; set; }
        public int? YPickPosition { get; set; }
        public int? StripNumber { get; set; }
        public int? TableNumber { get; set; }

        public int Data { get; set; }

        public EmapEntity() { }
    }
}
