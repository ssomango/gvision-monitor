using GVisionWpf.DomainLayer.Data;

namespace GVisionWpf.Repositories
{
    public class LgaRepository : RecipeRepository<LgaTeaching>
    {
        private static readonly Lazy<LgaRepository> lazy = new Lazy<LgaRepository>(() => new LgaRepository());
        public static LgaRepository Instance => lazy.Value;

        private LgaRepository() : base("LGA.rcp") { }

    }

    public class GridLgaRepository : RecipeRepository<GridLgaTeaching>
    {
        private static readonly Lazy<GridLgaRepository> lazy = new Lazy<GridLgaRepository>(() => new GridLgaRepository());
        public static GridLgaRepository Instance => lazy.Value;
        private GridLgaRepository() : base("GRID_LGA.rcp") { }
    }
}