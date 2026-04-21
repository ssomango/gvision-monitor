using GVisionWpf.DomainLayer.Data;

namespace GVisionWpf.Repositories
{
    public class QfnRepository : RecipeRepository<QfnTeaching>
    {
        private static readonly Lazy<QfnRepository> lazy = new Lazy<QfnRepository>(() => new QfnRepository());
        public static QfnRepository Instance => lazy.Value;

        private QfnRepository() : base("QFN.rcp") { }
    }

    public class GridQfnRepository : RecipeRepository<GridQfnTeaching>
    {
        private static readonly Lazy<GridQfnRepository> lazy = new Lazy<GridQfnRepository>(() => new GridQfnRepository());
        public static GridQfnRepository Instance => lazy.Value;

        private GridQfnRepository() : base("GRID_QFN.rcp") { }
    }
}