using GVisionWpf.DomainLayer.Data;

namespace GVisionWpf.Repositories
{
    public class BgaRepository : RecipeRepository<BgaTeaching>
    {
        private static readonly Lazy<BgaRepository> lazy = new Lazy<BgaRepository>(() => new BgaRepository());
        public static BgaRepository Instance => lazy.Value;

        private BgaRepository() : base("BGA.rcp") { }
    }

    public class GridBgaRepository : RecipeRepository<GridBgaTeaching>
    {
        private static readonly Lazy<GridBgaRepository> lazy = new Lazy<GridBgaRepository>(() => new GridBgaRepository());
        public static GridBgaRepository Instance => lazy.Value;

        private GridBgaRepository() : base("GRID_BGA.rcp") { }
    }
}