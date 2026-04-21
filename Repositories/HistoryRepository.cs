using System.Threading.Tasks;
using Dapper;
using GVisionWpf.Dialects.SqlLites;
using GVisionWpf.Models.Entities.History;

namespace GVisionWpf.Repositories
{
    public class HistoryRepository : EjfRepository<HistoryAntifragile, int>
    {
        private static readonly Lazy<HistoryRepository> lazy = new Lazy<HistoryRepository>(() => new HistoryRepository());
        public static HistoryRepository Instance => lazy.Value;

        private HistoryRepository() : base(DatabaseContext.Instance, "histories") { }

        public override async Task DeleteExpiredRecordsAsync()
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);
            string sql = $"DELETE FROM {this.tableName} WHERE Time IS NOT NULL AND Time < datetime('now', '-{DBSaveDays} days')";
            await wrapper.Connection.ExecuteAsync(sql);
        }
    }
}
