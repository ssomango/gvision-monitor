using System.Data;

namespace GVisionWpf.Dialects.SqlLites
{
    public class DisposableConnectionWrapper : IDisposable
    {
        private readonly DatabaseContext context;
        public IDbConnection Connection { get; private set; }

        public DisposableConnectionWrapper(DatabaseContext context)
        {
            this.context = context;
            Connection = context.CreateConnectionAsync().Result;
        }

        public void Dispose()
        {
            this.context.ReleaseConnection(Connection);
        }
    }
}