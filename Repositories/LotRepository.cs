using System.Threading.Tasks;
using Dapper;
using GVisionWpf.Dialects.SqlLites;
using GVisionWpf.Models.Entities.Lot;

namespace GVisionWpf.Repositories
{
    public class LotRepository : EjfRepository<LotAntifragile, int>
    {
        private static readonly Lazy<LotRepository> lazy = new Lazy<LotRepository>(() => new LotRepository());
        public static LotRepository Instance => lazy.Value;

        public LotRepository() : base(DatabaseContext.Instance, "lot") { }

        public override async Task DeleteExpiredRecordsAsync()
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);
            string sql = $"DELETE FROM {this.tableName} WHERE EndTime IS NOT NULL AND EndTime < datetime('now', '-{DBSaveDays} days')";
            await wrapper.Connection.ExecuteAsync(sql);
        }
    }
}
