namespace GVisionWpf.Repositories
{
    public class IlluminationRepository : RecipeRepository<IlluminationRecipe>
    {
        private static readonly Lazy<IlluminationRepository> lazy = new Lazy<IlluminationRepository>(() => new IlluminationRepository());
        public static IlluminationRepository Instance => lazy.Value;

        private IlluminationRepository() : base("Illumination.rcp") { }
    }
}