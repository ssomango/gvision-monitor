using System.Data.SQLite;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using GVisionWpf.Dialects.SqlLites;
using GVisionWpf.GlobalStates;

namespace GVisionWpf.Repositories
{
    public abstract class EjfRepository<T, TKeyType>
    {
        protected readonly DatabaseContext context;
        protected readonly string tableName;

        protected int DBSaveDays => GlobalSetting.Instance.Inspection.DBSaveDays;

        protected EjfRepository()
        {
            this.context = DatabaseContext.Instance;
        }

        protected EjfRepository(DatabaseContext context, string tableName)
        {
            this.context = context;
            this.tableName = tableName;

            DeleteExpiredRecordsAsync();
        }

        public async Task<IEnumerable<T>> FindAll()
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                string sql = $"SELECT * FROM {this.tableName} ORDER BY Id DESC";
                return await wrapper.Connection.QueryAsync<T>(sql);
            }
        }

        public async Task<IEnumerable<T>> FindAllBy(DynamicParameters parameters)
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);

            string sql = $"SELECT * FROM {this.tableName} WHERE 1=1";

            foreach (string paramName in parameters.ParameterNames)
            {
                sql += $" AND {paramName} = @{paramName}";
            }

            sql += " ORDER BY Id DESC";

            return await wrapper.Connection.QueryAsync<T>(sql, parameters);
        }

        public async Task<(IEnumerable<T>, int)> FindAllBy(int pageIndex, int pageSize)
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);

            DynamicParameters parameters = new DynamicParameters();

            string sql = $"SELECT * FROM {this.tableName} WHERE 1=1";
            string countSql = $"SELECT COUNT(*) FROM {this.tableName} WHERE 1=1";

            sql += $" ORDER BY Id DESC LIMIT @PageSize OFFSET @Offset";

            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (pageIndex - 1) * pageSize);

            var items = await wrapper.Connection.QueryAsync<T>(sql, parameters);
            var totalCount = await wrapper.Connection.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<(IEnumerable<T>, int)> FindAllBy(DynamicParameters parameters, int pageIndex, int pageSize)
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);

            string sql = $"SELECT * FROM {this.tableName} WHERE 1=1";
            string countSql = $"SELECT COUNT(*) FROM {this.tableName} WHERE 1=1";

            foreach (string paramName in parameters.ParameterNames)
            {
                sql += $" AND {paramName} = @{paramName}";
                countSql += $" AND {paramName} = @{paramName}";
            }

            sql += $" ORDER BY Id DESC LIMIT @PageSize OFFSET @Offset";

            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (pageIndex - 1) * pageSize);

            var items = await wrapper.Connection.QueryAsync<T>(sql, parameters);
            int totalCount = await wrapper.Connection.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<(IEnumerable<T>, int)> FindAllBy(DynamicParameters parameters, int pageIndex, int pageSize, DateTime startTime, DateTime endTime)
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);

            string sql = $"SELECT * FROM {this.tableName} WHERE 1=1";
            string countSql = $"SELECT COUNT(*) FROM {this.tableName} WHERE 1=1";

            foreach (string paramName in parameters.ParameterNames)
            {
                sql += $" AND {paramName} = @{paramName}";
                countSql += $" AND {paramName} = @{paramName}";
            }

            sql += $" AND Time BETWEEN @StartTime AND @EndTime";
            countSql += $" AND Time BETWEEN @StartTime AND @EndTime";

            sql += $" ORDER BY Id DESC LIMIT @PageSize OFFSET @Offset";

            parameters.Add("StartTime", startTime);
            parameters.Add("EndTime", endTime);
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (pageIndex - 1) * pageSize);

            var items = await wrapper.Connection.QueryAsync<T>(sql, parameters);
            int totalCount = await wrapper.Connection.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<IEnumerable<T>> FindAllByWithInCondition(DynamicParameters parameters, string[] inColumnNames)
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);

            string sql = $"SELECT * FROM {this.tableName} WHERE 1=1";

            foreach (string paramName in parameters.ParameterNames)
            {
                if (inColumnNames.Contains(paramName, StringComparer.OrdinalIgnoreCase))
                {
                    sql += $" AND {paramName} IN @{paramName}";
                }
                else
                {
                    sql += $" AND {paramName} = @{paramName}";
                }
            }

            sql += " ORDER BY Id DESC";

            return await wrapper.Connection.QueryAsync<T>(sql, parameters);
        }


        public async Task<T> FindById(TKeyType id)
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                string sql = $"SELECT * FROM {this.tableName} WHERE Id = @Id";
                return await wrapper.Connection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id });
            }
        }

        public async Task<T> Save(T entity)
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                Type type = typeof(T);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                List<string> parameter = new List<string>();
                List<string> value = new List<string>();

                foreach (PropertyInfo property in properties)
                {
                    if (property.Name == "Id") continue;

                    parameter.Add(property.Name);
                    value.Add("@" + property.Name);
                }

                string nameSql = string.Join(",", parameter);
                string valueSql = string.Join(",", value);

                string sql = $"INSERT INTO {this.tableName} ({nameSql}) VALUES ({valueSql}); SELECT last_insert_rowid();";

                int id = await wrapper.Connection.ExecuteScalarAsync<int>(sql, entity);
                var idProperty = type.GetProperty("Id");

                if (idProperty != null && idProperty.CanWrite)
                {
                    idProperty.SetValue(entity, id);
                }

                return entity;
            }
        }

        public async Task<int> SaveAll(List<T> entities)
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                if (entities.Count == 0)
                    return 0;

                Type type = typeof(T);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                List<string> parameterNames = new List<string>();
                List<string> valueNames = new List<string>();

                foreach (PropertyInfo property in properties)
                {
                    if (property.Name == "Id") continue;

                    parameterNames.Add(property.Name);
                    valueNames.Add("@" + property.Name);
                }

                string nameSql = string.Join(", ", parameterNames);
                string valueSql = string.Join(", ", valueNames);

                string sql = $"INSERT INTO {this.tableName} ({nameSql}) VALUES ({valueSql});";

                int result = await wrapper.Connection.ExecuteAsync(sql, entities);

                return result;
            }
        }

        public async Task Update(T entity)
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                Type type = typeof(T);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                List<string> parameter = new List<string>();

                foreach (PropertyInfo property in properties)
                {
                    parameter.Add($"{property.Name} = @{property.Name}");
                }

                string sql = @$"UPDATE {this.tableName} SET {string.Join(",", parameter)} WHERE Id = @Id";

                await wrapper.Connection.ExecuteAsync(sql, entity);
            }
        }

        public async Task Delete(TKeyType id)
        {
            using (var wrapper = new DisposableConnectionWrapper(this.context))
            {
                string sql = $"DELETE FROM {this.tableName} WHERE Id = @Id";
                await wrapper.Connection.ExecuteAsync(sql, new { Id = id });
            }
        }

        public async Task DeleteAll()
        {
            using var wrapper = new DisposableConnectionWrapper(this.context);
            string sql = $"DELETE FROM {this.tableName}";
            await wrapper.Connection.ExecuteAsync(sql);
        }

        public abstract Task DeleteExpiredRecordsAsync();
    }
}
