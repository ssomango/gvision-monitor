using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.Dialects.SqlLites
{
    public class ConnectionPool
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        private readonly List<SQLiteConnection> connections = new List<SQLiteConnection>();
        private static readonly Lazy<ConnectionPool> instance = new Lazy<ConnectionPool>(() => new ConnectionPool());

        private readonly SemaphoreSlim semaphore;

        public static ConnectionPool Instance => instance.Value;

        private ConnectionPool()
        {
            int maxConnections = 8;
            this.semaphore = new SemaphoreSlim(maxConnections, maxConnections);
            for (int i = 0; i < maxConnections; i++)
            {
                var connection = new SQLiteConnection(this.connectionString);
                connection.Open();
                this.connections.Add(connection);
            }
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            await this.semaphore.WaitAsync();
            lock (this.connections)
            {
                return this.connections.First(c => c.State == ConnectionState.Open);
            }
        }

        public void ReleaseConnection(IDbConnection connection)
        {
            lock (this.connections)
            {
                this.semaphore.Release();
            }
        }
    }
}