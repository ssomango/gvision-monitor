using Dapper;
using GVisionWpf.Dialects.SqlLites;
using GVisionWpf.Models.Entities.Emap;
using System.Threading.Tasks;

namespace GVisionWpf.Repositories
{
    public class EmapRepository : EjfRepository<EmapEntity, int>
    {
        private static readonly Lazy<EmapRepository> lazy = new Lazy<EmapRepository>(() => new EmapRepository());
        public static EmapRepository Instance => lazy.Value;

        public EmapRepository() : base(DatabaseContext.Instance, "emap") { }

        public async Task<int> GetStripNumber()
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                string sql = $"SELECT MAX(StripNumber) FROM {this.tableName};";
                int maxNumber = await wrapper.Connection.QuerySingleOrDefaultAsync<int?>(sql) ?? 0;

                return maxNumber;
            }
        }

        public override Task DeleteExpiredRecordsAsync() => Task.CompletedTask;
    }
}
