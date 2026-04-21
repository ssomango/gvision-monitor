using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using GVisionWpf.Dialects.SqlLites;
using GVisionWpf.Models.Entities.Lot;

namespace GVisionWpf.Repositories
{
    public class InspectionResultRepository : EjfRepository<InspectionResultAntifragile, int>
    {
        private static readonly Lazy<InspectionResultRepository> lazy = new Lazy<InspectionResultRepository>(() => new InspectionResultRepository());
        public static InspectionResultRepository Instance => lazy.Value;

        public InspectionResultRepository() : base(DatabaseContext.Instance, "inspection_results") { }

        public IEnumerable<ErrorCount> GetErrorCounts(int lotId, EInspection inspectionType)
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                string sql = @"
                    SELECT
                        Item AS ErrorType,
                        COUNT(*) AS Count
                    FROM
                        inspection_results
                    WHERE
                        LotId = @LotId AND InspectionType = @InspectionType
                    GROUP BY
                        Item

                    UNION ALL

                    SELECT
                        'Total' AS ErrorType,
                        COUNT(*) AS Count
                    FROM
                        inspection_results
                    WHERE
                        LotId = @LotId AND InspectionType = @InspectionType;
                ";

                return wrapper.Connection.Query<ErrorCount>(sql, new { LotId = lotId, InspectionType = inspectionType });
            }
        }

        public override async Task DeleteExpiredRecordsAsync()
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);
            string sql = $"DELETE FROM {this.tableName} WHERE EndTime IS NOT NULL AND EndTime < datetime('now', '-{DBSaveDays} days')";
            await wrapper.Connection.ExecuteAsync(sql);
        }
    }
}
