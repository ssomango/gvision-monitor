using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace GVisionWpf.DomainLayer.Data.Inspection.Item
{
    public interface IInspectionItem
    {
        public string Name { get; set; }
    }

    public abstract class InspectionItem : IInspectionItem
    {
        public string Name { get; set; }

        public InspectionItem(string name) => Name = name;

        public override string ToString() => Name;
    }
}
