using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Practical.GenericDbContextEFCore.Common;

namespace Practical.GenericDbContextEFCore.DbContext
{
    public interface IGenericDbContext<T> where T : Microsoft.EntityFrameworkCore.DbContext, IDisposable
    {
        DatabaseFacade Database { get; }
        DbSet<T> Repository<T>() where T : class;
        int SaveChanges();
        int SaveChanges(bool autoUpdateProperty = true);
        Task<int> SaveChangesAsync();
        void Dispose();
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
        Task<List<TEntity>> ExecuteStoreProcedureEntityAsync<TEntity>(string procedureName, IList<SqlParameter> parameters = null, int commandTimeOut = CommonConstants.CommandTimeout) where TEntity : class;
        Task<List<T>> ExecuteStoreProcedureAsync<T>(List<SqlParameter> sqlParameters, string storeProcedureName, int? timeout = null) where T : class;
        Task ExecuteStoreProcedureAsync(string storeProcedureName, List<SqlParameter> sqlParameters, int? timeout = null, Func<DbCommand, Task> hookExecutingAction = null, DbConnection wpConnection = null);
        T GetContext();
        T GetContext(bool isMultiThread = false);
        Dictionary<string, (int, int, int)> GetAddUpdateDeleteEntryCount();
        T GetContextScoped(IServiceScope serviceScope);
        Task<List<TEntity>> GetScopedResultAsync<TEntity>(IServiceProvider serviceProvider, Func<DbSet<TEntity>, Task<List<TEntity>>> queryFunc) where TEntity : class;
        void DetachAllEntities();
        void UpdateEntity<TEntity>(TEntity entity) where TEntity : class;
        void UpdateRangeEntity<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
        bool CreateTempTable(List<Guid> listWhereInIds, string tableName = "");
        bool DeleteTempTable(string tableName = "");
        Task<List<TEntity>> GetLargeWhereInSqlTempTableAsync<TEntity>(List<Guid> listWhereInIds, Func<bool, List<TEntity>> func, int maxWhereIn = CommonConstants.MinimumIdsForCreateTempTable);
        List<TEntity> GetLargeWhereInSqlTempTable<TEntity>(List<Guid> listWhereInIds, Func<bool, List<TEntity>> func, int maxWhereIn = CommonConstants.MinimumIdsForCreateTempTable);
        Task<List<TEntity>> GetLargeWhereInSqlTempTableAsync<TEntity>(List<Guid> listWhereInIds, Func<bool, Task<List<TEntity>>> func, int maxWhereIn = CommonConstants.MinimumIdsForCreateTempTable);
        void BulkInsertUpdateDeleteEntities<TInsert, TUpdate>(List<TInsert> insertedEntities, List<TUpdate> updatedEntities, List<Guid> deletedEntities, string tableSchemasAndName);
        Task<int> BulkInsertUpdateDeleteEntitiesAsync<TInsert, TUpdate>(List<TInsert> insertedEntities, List<TUpdate> updatedEntities, List<Guid> deletedEntities, string tableSchemasAndName);
        Task<int> BulkInsertAsync<TEntity>(List<TEntity> entityParent, string? schemas = null, bool detachedAfterComplete = false, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        Task<int> BulkInsertDataTableWithoutTransactionAsync(DataTable dataTable, string tableName, SqlConnection sqlConnection, List<SqlBulkCopyColumnMapping> listColumnMappings, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        Task<int> BulkInsertAsync<T1, T2>(List<T1> entityParent, List<T2> entityChildren, string? schemas = null, bool detachedAfterComplete = true, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        Task<int> BulkInsertAsync<T1, T2, T3>(List<T1> entityParent, List<T2> entityChildren1, List<T3> entityChildren2, bool detachedAfterComplete = true, string? schemas = null, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        Task<int> BulkInsertAsync<TEntity>(List<TEntity> entities, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        Task<int> BulkUpdateAsync<TEntity>(List<TEntity> entities, string? schemas = null, List<string>? columns = null, bool detachedAfterComplete = true, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        Task<int> BulkDeleteAsync<TEntity>(List<Guid> guids, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        int BulkUpdate<TEntity>(List<TEntity> entities, string schemas, List<string>? columns = null, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        int BulkInsert<TEntity>(List<TEntity> entities, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        int BulkDelete<TEntity>(List<Guid> guids, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize);
        int ExecuteSqlRaw(string sql, params object[] parameters);
        string GetEntityChangesSummary();
        string GetConnectionString();
        void DetachAllEntities(List<string> detachEntities);
        void DetachAllEntitiesByIds<TEntity>(List<Guid> detachEntitieIds);
        (List<TEntity>, List<TEntity>, List<TEntity>) GetAddUpdateDeleteEntries<TEntity>();
        Task<(bool, Y)> ExecuteTransactionAsync<Y>(Func<Task<Y>> func, IsolationLevel? isolationLevel = null);
        Task<bool> IsExistTableAsync(string tableNameAndSchema);
        void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
        string GetDbName();
    }
}
