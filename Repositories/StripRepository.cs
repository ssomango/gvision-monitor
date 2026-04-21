namespace GVisionWpf.Repositories
{
    public class StripRepository : RecipeRepository<StripTeaching>
    {
        private static readonly Lazy<StripRepository> lazy = new Lazy<StripRepository>(() => new StripRepository());
        public static StripRepository Instance => lazy.Value;

        private StripRepository() : base("STRIP.rcp") { }
    }
}
