namespace GVisionWpf.Repositories
{
    public class MoldRepository : RecipeRepository<MoldTeaching>
    {
        private static readonly Lazy<MoldRepository> lazy = new Lazy<MoldRepository>(() => new MoldRepository());
        public static MoldRepository Instance => lazy.Value;

        private MoldRepository() : base("Mold.rcp") { }
    }

    public class GridMoldRepository : RecipeRepository<GridMoldTeaching>
    {
        private static readonly Lazy<GridMoldRepository> lazy = new Lazy<GridMoldRepository>(() => new GridMoldRepository());
        public static GridMoldRepository Instance => lazy.Value;

        private GridMoldRepository() : base("GRID_MOLD.rcp") { }
    }
}