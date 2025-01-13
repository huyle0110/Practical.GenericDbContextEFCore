using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Data;
using Practical.GenericDbContextEFCore.Service.Interface;
using Practical.GenericDbContextEFCore.Common;
using Practical.GenericDbContextEFCore.Extension;
using Serilog;

namespace Practical.GenericDbContextEFCore.DbContext
{
    public class GenericDbContext<T> : IGenericDbContext<T> where T : BaseDbContext, IDisposable
    {
        // Flag: Has Dispose already been called? recheck solution???
        private bool disposed = false;

        private readonly T _dbContext;

        private readonly ICacheData _cacheData;
        private string _connectionString;

        public GenericDbContext(T dataContext, ICacheData cacheData)
        {
            _dbContext = dataContext;
            _cacheData = cacheData;
                _connectionString = dataContext.Database.GetDbConnection().ConnectionString;
        }

        public DatabaseFacade Database { get { return _dbContext.Database; } }

        public DbSet<TEntity> Repository<TEntity>() where TEntity : class
        {
            return _dbContext.Repository<TEntity>();
        }

        public int SaveChanges()
        {
            var isEmptyCache = _cacheData == null || _cacheData.IsEmpty();

            var changedEntries = isEmptyCache ? null : _dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .GroupBy(e => e.Metadata.Name)
                .Select(g => g.First())
                .Select(e => e.Entity.GetType());

            var result = _dbContext.SaveChanges();

            if (!isEmptyCache) {
                _cacheData?.Clear(changedEntries);
            }

            return result;
        }

        public int SaveChanges(bool autoUpdateProperty = true)
        {
            if (autoUpdateProperty) {
                return _dbContext.SaveChanges();
            }
            return _dbContext.SaveChangesWithoutUpdateProperty();
        }

        public async Task<int> SaveChangesAsync()
        {
            var isEmptyCache = _cacheData == null || _cacheData.IsEmpty();

            var changedEntries = isEmptyCache ? null : _dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .GroupBy(e => e.Metadata.Name)
                .Select(g => g.First())
                .Select(e => e.Entity.GetType());

            var result = await _dbContext.SaveChangesAsync();

            if (!isEmptyCache) {
                _cacheData?.Clear(changedEntries);
            }

            return result;
        }

        /// <summary>
        /// Bulk insert entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<int> BulkInsertAsync<TEntity>(List<TEntity> entities, string? schemas = null, bool detachedAfterComplete = true, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (entities == null || !entities.Any())
                return await Task.FromResult(0);

            string reldbSchemas = _dbContext.GetSchemaOfTableName<TEntity>();
            var dbSchemas = string.IsNullOrEmpty(schemas) ? (string.IsNullOrEmpty(reldbSchemas) ? _dbContext.GetSchemas() : reldbSchemas) : schemas;

            int rowEffects = _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                return sqlCnn.AddBulkInsert(trans, sqlCmd, entities, dbSchemas, batchSize, _dbContext.GetTableName<TEntity>());
            });
            if (detachedAfterComplete)
                _dbContext.DetachAllEntitiesByType(typeof(TEntity));

            return await Task.FromResult(rowEffects);
        }

        /// <summary>
        /// Bulk Insert DataTable
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dataTable"></param>
        /// <param name="tableName"></param>
        /// <param name="schemas"></param>
        /// <param name="detachedAfterComplete"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<int> BulkInsertDataTableWithoutTransactionAsync(DataTable dataTable, string tableName, SqlConnection sqlConnection, List<SqlBulkCopyColumnMapping> listColumnMappings, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return await Task.FromResult(0);

            int rowEffects = _dbContext.BulkSaveChangeWithoutTransaction((sqlCnn, sqlCmd) =>
            {
                return sqlCnn.AddBulkInsertDataTable(dataTable, tableName, listColumnMappings, batchSize);
            }, sqlConnection);

            return await Task.FromResult(rowEffects);
        }

        /// <summary>
        /// Bulk update entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<int> BulkUpdateAsync<TEntity>(List<TEntity> entities, string? schemas = null, List<string>? columns = null, bool detachedAfterComplete = true, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (entities == null || !entities.Any())
                return await Task.FromResult(0);

            string reldbSchemas = _dbContext.GetSchemaOfTableName<TEntity>();
            var dbSchemas = string.IsNullOrEmpty(schemas) ? (string.IsNullOrEmpty(reldbSchemas) ? _dbContext.GetSchemas() : reldbSchemas) : schemas;

            int rowEffects = _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                return sqlCnn.AddBulkUpdate(trans, sqlCmd, entities, dbSchemas, columns, batchSize, _dbContext.GetTableName<TEntity>());
            });
            if (detachedAfterComplete)
                _dbContext.DetachAllEntitiesByType(typeof(TEntity));

            return await Task.FromResult(rowEffects);
        }

        /// <summary>
        /// Bulk insert parent - child entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<int> BulkInsertAsync<T1, T2>(List<T1> entityParent, List<T2> entityChildren, string? schemas = null, bool detachedAfterComplete = true, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            int rowEffects = 0;
            string reldbSchemasT1 = _dbContext.GetSchemaOfTableName<T1>();
            var dbSchemasT1 = string.IsNullOrEmpty(schemas) ? (string.IsNullOrEmpty(reldbSchemasT1) ? _dbContext.GetSchemas() : reldbSchemasT1) : schemas;
            string reldbSchemasT2 = _dbContext.GetSchemaOfTableName<T2>();
            var dbSchemasT2 = string.IsNullOrEmpty(schemas) ? (string.IsNullOrEmpty(reldbSchemasT2) ? _dbContext.GetSchemas() : reldbSchemasT2) : schemas;
            rowEffects = _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                int rows = 0;
                rows += sqlCnn.AddBulkInsert(trans, sqlCmd, entityParent, dbSchemasT1, batchSize, _dbContext.GetTableName<T1>());
                rows += sqlCnn.AddBulkInsert(trans, sqlCmd, entityChildren, dbSchemasT2, batchSize, _dbContext.GetTableName<T2>());
                return rows;
            });
            if (detachedAfterComplete)
                _dbContext.DetachAllEntitiesByType(typeof(T1), typeof(T2));

            return await Task.FromResult(rowEffects);
        }

        /// <summary>
        /// Bulk insert parent - child entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<int> BulkInsertAsync<T1, T2, T3>(List<T1> entityParent, List<T2> entityChildren1, List<T3> entityChildren2, bool detachedAfterComplete = true, string? schemas = null, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            int rowEffects = 0;
            string reldbSchemasT1 = _dbContext.GetSchemaOfTableName<T1>();
            string reldbSchemasT2 = _dbContext.GetSchemaOfTableName<T2>();
            string reldbSchemasT3 = _dbContext.GetSchemaOfTableName<T3>();

            var dbSchemasT1 = string.IsNullOrEmpty(schemas) ? (string.IsNullOrEmpty(reldbSchemasT1) ? _dbContext.GetSchemas() : reldbSchemasT1) : schemas;
            var dbSchemasT2 = string.IsNullOrEmpty(schemas) ? (string.IsNullOrEmpty(reldbSchemasT2) ? _dbContext.GetSchemas() : reldbSchemasT2) : schemas;
            var dbSchemasT3 = string.IsNullOrEmpty(schemas) ? (string.IsNullOrEmpty(reldbSchemasT3) ? _dbContext.GetSchemas() : reldbSchemasT3) : schemas;

            rowEffects = _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                int rows = 0;
                rows += sqlCnn.AddBulkInsert(trans, sqlCmd, entityParent, dbSchemasT1, batchSize, _dbContext.GetTableName<T1>());
                rows += sqlCnn.AddBulkInsert(trans, sqlCmd, entityChildren1, dbSchemasT2, batchSize, _dbContext.GetTableName<T2>());
                rows += sqlCnn.AddBulkInsert(trans, sqlCmd, entityChildren2, dbSchemasT3, batchSize, _dbContext.GetTableName<T3>());
                return rows;
            });
            if (detachedAfterComplete)
                _dbContext.DetachAllEntitiesByType(typeof(T1), typeof(T2), typeof(T3));
            return await Task.FromResult(rowEffects);
        }

        /// <summary>
        /// Bulk insert entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<int> BulkInsertAsync<TEntity>(List<TEntity> entities, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (entities == null || !entities.Any())
                return await Task.FromResult(0);

            int rowEffects = _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                return sqlCnn.AddBulkInsert(trans, sqlCmd, entities, schemas, batchSize, _dbContext.GetTableName<TEntity>());
            });
            return await Task.FromResult(rowEffects);
        }

        /// <summary>
        /// Bulk delete entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<int> BulkDeleteAsync<TEntity>(List<Guid> guids, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (guids == null || !guids.Any())
                return await Task.FromResult(0);

            int rowEffects = _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                return sqlCnn.AddBulkDelete<TEntity>(trans, sqlCmd, guids, schemas, batchSize, _dbContext.GetTableName<TEntity>());
            });
            return await Task.FromResult(rowEffects);
        }

        /// <summary>
        /// Detach all entities in data context
        /// </summary>
        public void DetachAllEntities()
        {
            var changedEntriesCopy = _dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing) {
                _dbContext.Dispose();
            }
            disposed = true;
        }

        public T GetContext()
        {
            return _dbContext;
        }

        public T GetContext(bool isMultiThread = false)
        {
            if (!isMultiThread) return _dbContext;
            return _dbContext;
        }

        /// <summary>
        /// execute row sql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            return await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        /// <summary>
        /// Execute stored procedure 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="procedureName">Stored procedure name</param>
        /// <param name="parameters">List of parameters</param>
        /// <param name = "commandTimeOut" >Command Time Out</param>
        /// <returns></returns>
        public async Task<List<TEntity>> ExecuteStoreProcedureEntityAsync<TEntity>(string procedureName, IList<SqlParameter> parameters = null, int commandTimeOut = CommonConstants.CommandTimeout) where TEntity : class
        {
            var dbSet = _dbContext.Set<TEntity>();
            SqlParameter[] sqlParameters = Array.Empty<SqlParameter>();
            if (parameters != null && parameters.Any()) {
                var parameternames = new List<string>();
                foreach (var parameter in parameters) {
                    var parametername = $"{parameter.ParameterName}";
                    if (!parameter.ParameterName.Contains(CommonConstants.ParameterProcedureAtSign)) parametername = $"{CommonConstants.ParameterProcedureAtSign}{parametername}";
                    if (parameter.Direction == ParameterDirection.Output) parametername = $"{parametername} {CommonConstants.ParameterProcedureOutput}";
                    parameternames.Add(parametername);
                }
                procedureName += $" {string.Join(CommonConstants.TextComma, parameternames)}";
                sqlParameters = parameters.ToArray();
            }
            _dbContext.Database.SetCommandTimeout(commandTimeOut);
            return await dbSet.FromSqlRaw(procedureName, sqlParameters).ToListAsync();
        }

        /// <summary>
        /// Execute Store Procedure and convert result to list TEntity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlParameters"></param>
        /// <param name="storeProcedureName"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteStoreProcedureAsync<T>(List<SqlParameter> sqlParameters, string storeProcedureName, int? timeout = null) where T : class
        {
            var result = new List<T>();
            try {
                if (!string.IsNullOrEmpty(storeProcedureName)) {
                    T obj = default(T);
                    var dbConnection = Database.GetDbConnection();
                    using (var command = dbConnection.CreateCommand()) {
                        command.CommandText = storeProcedureName;
                        command.CommandType = CommandType.StoredProcedure;

                        if (sqlParameters != null && sqlParameters.Any()) {
                            command.Parameters.AddRange(sqlParameters.ToArray());
                        }

                        command.CommandTimeout = timeout ?? CommonConstants.CommandTimeout;

                        // Open connection if its state is Closed
                        if (dbConnection.State == ConnectionState.Closed) {
                            await Database.OpenConnectionAsync();
                        }

                        using (var reader = await command.ExecuteReaderAsync()) {
                            var columnsResult = Enumerable.Range(0, reader.FieldCount).Select(x => reader.GetName(x)).ToList();
                            while (reader.Read()) {
                                //parse view model from sp reader
                                obj = Activator.CreateInstance<T>();
                                foreach (var property in obj.GetType().GetProperties()) {
                                    //Check column existed in reader
                                    var existColumn = columnsResult.Any(x => string.Equals(x, property.Name, StringComparison.OrdinalIgnoreCase));
                                    if (existColumn) {
                                        var value = reader.GetValue(property.Name);
                                        if (!(value is null || DBNull.Value.Equals(value))) {
                                            Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                                            object safeValue = (value == null) ? null : Convert.ChangeType(value, t);
                                            property.SetValue(obj, safeValue, null);
                                        }
                                    }
                                }
                                result.Add(obj);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex) {
                Log.Logger.Error($"exec {storeProcedureName} is error: {ex.Message}.");
                throw;
            }
        }

        /// <summary>
        /// Execute Store Procedure no return result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlParameters"></param>
        /// <param name="storeProcedureName"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task ExecuteStoreProcedureAsync(string storeProcedureName, List<SqlParameter> sqlParameters, int? timeout = null, Func<DbCommand, Task> hookExecutingAsync = null, DbConnection wpConnection = null)
        {
            try {
                if (!string.IsNullOrEmpty(storeProcedureName)) {
                    var dbConnection = wpConnection;
                    if (dbConnection == null) {
                        dbConnection = Database.GetDbConnection();
                    }
                    using (var command = dbConnection.CreateCommand()) {
                        command.CommandText = storeProcedureName;
                        command.CommandType = CommandType.StoredProcedure;

                        if (sqlParameters != null && sqlParameters.Any()) {
                            command.Parameters.AddRange(sqlParameters.ToArray());
                        }

                        command.CommandTimeout = timeout ?? CommonConstants.CommandTimeout;

                        // Open connection if its state is Closed
                        if (dbConnection.State == ConnectionState.Closed) {
                            await Database.OpenConnectionAsync();
                        }

                        if (hookExecutingAsync == null) {
                            await command.ExecuteNonQueryAsync();
                        }
                        else {
                            await hookExecutingAsync(command);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Logger.Error($"exec {storeProcedureName} is error: {ex.Message}.");
                throw;
            }
        }

        /// <summary>
        /// execute row sql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public int ExecuteSqlRaw(string sql, params object[] parameters)
        {
            return _dbContext.Database.ExecuteSqlRaw(sql, parameters);
        }

        /// <summary>
        /// Get detail add/update/delete informations
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, (int, int, int)> GetAddUpdateDeleteEntryCount()
        {
            var entities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged);
            Dictionary<string, (int, int, int)> dic = new Dictionary<string, (int, int, int)>();
            entities.GroupBy(x => x.Entity.GetType().Name).ToList().ForEach(group =>
            {
                var key = group.First().Entity.GetType().Name;
                var value = (
                    group.Where(x => x.State == EntityState.Added).Count(),
                    group.Where(x => x.State == EntityState.Modified).Count(),
                    group.Where(x => x.State == EntityState.Deleted).Count()
                );
                dic.Add(key, value);
            });
            return dic;
        }

        /// <summary>
        /// Get instance of dbcontext in a serviceScope identified by type of 'T'.
        /// </summary>
        public T GetContextScoped(IServiceScope serviceScope)
        {
            if (serviceScope != null) {
                return serviceScope.ServiceProvider.GetRequiredService<T>();
            }

            return GetContext();
        }

        /// <summary>
        /// Execute a wrapper query 'queryFunc' in a serviceScope and return a list of Entity data.
        /// </summary>
        public async Task<List<TEntity>> GetScopedResultAsync<TEntity>(IServiceProvider serviceProvider, Func<DbSet<TEntity>, Task<List<TEntity>>> queryFunc) where TEntity : class
        {
            using (var serviceScope = serviceProvider.CreateScope()) {
                var dbContextScoped = GetContextScoped(serviceScope);
                var result = await queryFunc(dbContextScoped.Repository<TEntity>());
                return result;
            }
        }

        /// <summary>
        /// Update entity for tune up sql
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void UpdateEntity<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity != null) {
                _dbContext.UpdateEntity(entity);
            }
        }

        /// <summary>
        /// Update engity range. Each entity must specify ChangedFields to save 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        public void UpdateRangeEntity<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            if (entities != null && entities.Any()) {
                _dbContext.UpdateRangeEntity(entities);
            }
        }

        /// <summary>
        /// Create temp table 
        /// </summary>
        /// <param name="listWhereInIds"></param>
        /// <returns></returns>
        public bool CreateTempTable(List<Guid> listWhereInIds, string tableName = "")
        {
            var tempTableName = string.IsNullOrEmpty(tableName) ? $"#{nameof(TempTableData)}" : tableName;
            return DBContextExtension.CreateTempTable(_dbContext, tempTableName, listWhereInIds);
        }

        /// <summary>
        /// Delete temp table
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool DeleteTempTable(string tableName = "")
        {
            if (string.IsNullOrEmpty(tableName)) {
                tableName = $"#{nameof(TempTableData)}";
            }
            return DBContextExtension.DeleteTempTable(_dbContext, tableName);
        }

        /// <summary>
        /// Wrapper get large where in sql
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="listWhereInIds">list join</param>
        /// <param name="func">function for get data</param>
        /// <param name="maxWhereIn">apply when list id larger than 200</param>
        /// <returns></returns>
        public async Task<List<TEntity>> GetLargeWhereInSqlTempTableAsync<TEntity>(List<Guid> listWhereInIds, Func<bool, List<TEntity>> func, int maxWhereIn = CommonConstants.MinimumIdsForCreateTempTable)
        {
            return await _dbContext.GetLargeWhereInSqlTempTableAsync(listWhereInIds, func, maxWhereIn);
        }

        /// <summary>
        /// Wrapper get large where in sql
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="listWhereInIds"></param>
        /// <param name="func"></param>
        /// <param name="maxWhereIn"></param>
        /// <returns></returns>
        public async Task<List<TEntity>> GetLargeWhereInSqlTempTableAsync<TEntity>(List<Guid> listWhereInIds, Func<bool, Task<List<TEntity>>> func, int maxWhereIn = CommonConstants.MinimumIdsForCreateTempTable)
        {
            return await _dbContext.GetLargeWhereInSqlTempTableAsync(listWhereInIds, func, maxWhereIn);
        }

        /// <summary>
        /// Wrapper get large where in sql
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="listWhereInIds">list join</param>
        /// <param name="func">function for get data</param>
        /// <param name="maxWhereIn">apply when list id larger than 200</param>
        /// <returns></returns>
        public List<TEntity> GetLargeWhereInSqlTempTable<TEntity>(List<Guid> listWhereInIds, Func<bool, List<TEntity>> func, int maxWhereIn = CommonConstants.MinimumIdsForCreateTempTable)
        {
            return _dbContext.GetLargeWhereInSqlTempTable(listWhereInIds, func, maxWhereIn);
        }

        /// <summary>
        /// Bulk update entity
        /// </summary>
        /// <typeparam name="TEntityModel"></typeparam>
        /// <param name="updatedEntities"></param>
        /// <param name="tableSchemasAndName"></param>
        public void BulkInsertUpdateDeleteEntities<TInsert, TUpdate>(List<TInsert> insertedEntities, List<TUpdate> updatedEntities, List<Guid> deletedEntities, string tableSchemasAndName)
        {
            DBContextExtension.BulkInsertUpdateDeleteEntities(_connectionString, insertedEntities, updatedEntities, deletedEntities, tableSchemasAndName);
        }

        /// <summary>
        /// Bulk Insert Update Delete Entities
        /// </summary>
        /// <typeparam name="TInsert"></typeparam>
        /// <typeparam name="TUpdate"></typeparam>
        /// <param name="insertedEntities"></param>
        /// <param name="updatedEntities"></param>
        /// <param name="deletedEntities"></param>
        /// <param name="tableSchemasAndName"></param>
        /// <returns></returns>
        public async Task<int> BulkInsertUpdateDeleteEntitiesAsync<TInsert, TUpdate>(List<TInsert> insertedEntities, List<TUpdate> updatedEntities, List<Guid> deletedEntities, string tableSchemasAndName)
        {
            return await DBContextExtension.BulkInsertUpdateDeleteEntitiesAsync(_connectionString, insertedEntities, updatedEntities, deletedEntities, tableSchemasAndName);
        }

        /// <summary>
        /// Summary the detail update of each data table 
        /// </summary>
        /// <returns></returns>
        public string GetEntityChangesSummary()
        {
            var entitiesChanges = GetAddUpdateDeleteEntryCount();
            var dbContextName = _dbContext.GetType().Name;
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (entitiesChanges != null && entitiesChanges.Any()) {
                builder.AppendLine($"Changes on {dbContextName}:");
                foreach (var item in entitiesChanges.OrderBy(x => x.Key)) {
                    builder.AppendLine($" - Table {item.Key}: Add: {item.Value.Item1} rows, update {item.Value.Item2} rows, delete {item.Value.Item3} rows");
                }
            }
            else {
                builder.AppendLine($"No changes on {dbContextName}");
            }

            return builder.ToString();
        }

        public string GetConnectionString()
        {
            return _dbContext.Database.GetDbConnection().ConnectionString;
        }


        /// <summary>
        /// Get detail add/update/delete entities
        /// </summary>
        /// <returns></returns>
        public (List<TEntity>, List<TEntity>, List<TEntity>) GetAddUpdateDeleteEntries<TEntity>()
        {
            return _dbContext.GetAddUpdateDeleteEntries<TEntity>();
        }

        /// <summary>
        /// Detach entities
        /// </summary>
        /// <param name="entityStates"></param>
        public void DetachAllEntities(List<string> detachEntities)
        {
            _dbContext.DetachAllEntities(detachEntities);
        }

        /// <summary>
        /// Bulk update entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public int BulkUpdate<TEntity>(List<TEntity> entities, string schemas, List<string>? columns = null, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (entities == null || !entities.Any())
                return 0;

            return _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                return sqlCnn.AddBulkUpdate(trans, sqlCmd, entities, schemas, columns, batchSize, _dbContext.GetTableName<TEntity>());
            });
        }

        /// <summary>
        /// Bulk insert entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public int BulkInsert<TEntity>(List<TEntity> entities, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (entities == null || !entities.Any())
                return 0;

            return _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                return sqlCnn.AddBulkInsert(trans, sqlCmd, entities, schemas, batchSize, _dbContext.GetTableName<TEntity>());
            });
        }

        /// <summary>
        /// Bulk delete entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableAndNameSchemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public int BulkDelete<TEntity>(List<Guid> guids, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (guids == null || !guids.Any())
                return 0;

            return _dbContext.BulkSaveChangeWithTransactions((sqlCnn, trans, sqlCmd) =>
            {
                return sqlCnn.AddBulkDelete<TEntity>(trans, sqlCmd, guids, schemas, batchSize, _dbContext.GetTableName<TEntity>());
            });
        }

        /// <summary>
        /// Execute Transaction Async
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="func"></param>
        /// <param name="isolationLevel"></param>
        /// <seealso cref="https://learn.microsoft.com/en-us/dotnet/api/system.data.isolationlevel?view=net-6.0"/>
        /// <seealso cref="https://learn.microsoft.com/en-us/ef/core/saving/transactions"/>
        /// <seealso cref="https://learn.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql?view=sql-server-ver16"/>
        /// <returns></returns>
        public async Task<(bool, Y)> ExecuteTransactionAsync<Y>(Func<Task<Y>> func, IsolationLevel? isolationLevel = null)
        {
            Y result = default;
            bool isCommitted = false;

            // Create ExecutionStrategy
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                try {
                    if (isolationLevel != null) {
                        await _dbContext.Database.BeginTransactionAsync(isolationLevel.Value);
                    }
                    else {
                        await _dbContext.Database.BeginTransactionAsync();
                    }

                    // Invoke oprations
                    result = await func.Invoke();

                    // Save & commit data
                    await _dbContext.SaveChangesAsync();
                    await _dbContext.Database.CommitTransactionAsync();
                    isCommitted = true;
                }
                catch {
                    // Rollback data
                    await _dbContext.Database.RollbackTransactionAsync();
                    isCommitted = false;
                }
            });

            return (isCommitted, result);
        }

        /// <summary>
        /// Detach entities
        /// </summary>
        /// <param name="entityStates"></param>
        public void DetachAllEntitiesByIds<TEntity>(List<Guid> detachEntitieIds)
        {
            var changedEntriesCopy = _dbContext.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged
                                && e.Entity.GetType().Name == typeof(TEntity).Name
                                && detachEntitieIds.Contains(((Guid?)e.Entity.GetValueIgnoreCase("Id")).GetValueOrDefault()))
                    .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        /// <summary>
        /// Check if table is exist
        /// </summary>
        /// <param name="tableNameAndSchema"></param>
        /// <returns></returns>
        public async Task<bool> IsExistTableAsync(string tableNameAndSchema)
        {
            return await DBContextExtension.IsDbTableExistAsync(tableNameAndSchema, _connectionString);
        }

        /// <summary>
        /// Add range of entities
        /// </summary>
        /// <param name="entities"></param>
        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            if (entities != null && entities.Any()) {
                _dbContext.AddRange(entities);
            }
        }

        /// <summary>
        /// Get database name
        /// </summary>
        public string GetDbName()
            => _dbContext.Database?.GetDbConnection()?.Database ?? string.Empty;
    }
}
