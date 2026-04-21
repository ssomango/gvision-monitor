using System.Data;
using System.Threading.Tasks;

namespace GVisionWpf.Dialects.SqlLites
{
    public class DatabaseContext
    {
        private static readonly Lazy<DatabaseContext> lazy = new Lazy<DatabaseContext>(() => new DatabaseContext());
        public static DatabaseContext Instance => lazy.Value;

        private DatabaseContext() { }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            return await ConnectionPool.Instance.GetConnectionAsync();
        }

        public void ReleaseConnection(IDbConnection connection)
        {
            ConnectionPool.Instance.ReleaseConnection(connection);
        }
    }
}
