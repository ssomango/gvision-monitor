using GVisionWpf.Illuminations;

namespace GVisionWpf.Models.Entities.Recipe
{
    public class IlluminationRecipe
    {
        public bool IsTaught = false;
        public Dictionary<ECamera, List<Dictionary<ELight, int>>> Setting = new Dictionary<ECamera, List<Dictionary<Illuminations.ELight, int>>>();
    }
}
