using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Practical.GenericDbContextEFCore.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Practical.GenericDbContextEFCore.Extension
{
    public class BulkInsertConfig
    {
        public string CustomTableName { get; set; }
        public string TableSchemas { get; set; }
        public int BatchSize { get; set; } = CommonConstants.SqlBulkCopyBatchSize;
        public List<string> ColumnUpdates { get; set; }
        public List<Type> ListEntityNeedDetached { get; set; } = new List<Type>();

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(TableSchemas);
        }

        public BulkInsertConfig Clone(BulkInsertConfig config)
        {
            return new BulkInsertConfig
            {
                CustomTableName = config.CustomTableName,
                TableSchemas = config.TableSchemas,
                BatchSize = config.BatchSize
            };
        }
    }
    
    public static class DBContextExtension
    {
        /// <summary>
        /// Check exception is deadlock
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static bool IsDeadLockException(Exception ex)
        {
            var sqlException = ex as SqlException;
            return (sqlException != null && (sqlException.Number == 1205 || sqlException.Message.Contains("deadlocked"))) || ex.Message.Contains("deadlocked");
        }

        /// <summary>
        /// DetachAllEntities
        /// </summary>
        /// <param name="dbDbContext"></param>
        public static void DetachAllEntities(this Microsoft.EntityFrameworkCore.DbContext dbDbContext)
        {
            var changedEntriesCopy = dbDbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        public static int BulkSaveChangeWithTransactions(this Microsoft.EntityFrameworkCore.DbContext dbDbContext, Func<SqlConnection, SqlTransaction, SqlCommand, int> action, bool detachedAfterComplete = false)
        {
            return RetryExtension.DoRetryReturnResult(() =>
            {
                int rowEffects = 0;
                var sqlConn = (SqlConnection)dbDbContext.Database.GetDbConnection();
                if (sqlConn.State != ConnectionState.Open)
                {
                    sqlConn.Open();
                }
                using (SqlTransaction trans = sqlConn.BeginTransaction())
                {
                    using (SqlCommand cmd = new SqlCommand(string.Empty, sqlConn, trans))
                    {
                        cmd.CommandTimeout = CommonConstants.CommandTimeout;
                        try
                        {
                            rowEffects = action.Invoke(sqlConn, trans, cmd);
                            // insert 

                            trans.Commit();

                            if (detachedAfterComplete)
                            {
                                dbDbContext.DetachAllEntities();
                            }
                        }
                        catch (Exception ex) {
                            trans.Rollback();
                            throw;
                        }
                        finally {
                            sqlConn.Close();
                        }
                    }
                }
                return rowEffects;
            }, (ex) => IsDeadLockException(ex), TimeSpan.FromSeconds(10), 3);
        }

        public static string GetSchemaOfTableName<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            return dbContext.Model.FindEntityType(typeof(TEntity))?.GetSchema() ?? "dbo";
        }

        /// <summary>
        /// Only update changed fields
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entity"></param>
        /// <param name="changedFields"></param>
        /// <param name="defaultFieldsUpdated"></param>
        public static void HandleEntityUpdateChangedFields<TContext, TEntity>(this TContext dbContext, TEntity entity, IList<string> changedFields, string[] defaultFieldsUpdated) where TContext : Microsoft.EntityFrameworkCore.DbContext where TEntity : class
        {
            if (entity == null) return;
            var entry = dbContext.Entry(entity);
            var isBeingTracked = entry.State == EntityState.Unchanged || entry.State == EntityState.Modified;

            if (changedFields?.Count > 0) {
                if (defaultFieldsUpdated?.Length > 0) {
                    changedFields = changedFields.Concat(defaultFieldsUpdated).Distinct().ToList();
                }

                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.IsKey()) continue;
                    var fieldChanged = changedFields.FirstOrDefault(f => f.Equals(property.Metadata.Name, StringComparison.OrdinalIgnoreCase));
                    if (fieldChanged != null) {
                        property.IsModified = true;
                    }
                    else {
                        if (isBeingTracked && property.IsModified) {
                            property.IsModified = false;
                        }
                    }
                }
            }
            else {
                if (isBeingTracked) {
                    if (entry.State == EntityState.Modified) {
                        entry.State = EntityState.Unchanged;
                    }
                }
            }
        }

        /// <summary>
        /// clone list object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listObj"></param>
        /// <returns></returns>
        public static List<T> DeepCloneEntity<T>(this List<T> listObj)
        {
            var result = new List<T>();
            foreach (var item in listObj) {
                result.Add((T)CloneEntityObject(item));
            }
            return result;
        }

        /// <summary>
        /// clone entity
        /// </summary>
        /// <param name="objSource"></param>
        /// <returns></returns>
        public static object CloneEntityObject(object objSource)
        {
            var type = objSource.GetType();
            var target = Activator.CreateInstance(type);
            var propInfo = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo prop in propInfo) {
                if (prop.CanWrite && prop.CanRead) {
                    if (prop.PropertyType.IsValueType || prop.PropertyType.IsEnum || prop.PropertyType.Equals(typeof(String))) {
                        prop.SetValue(target, prop.GetValue(objSource, null), null);
                    }
                    else {
                        object objPropertyValue = prop.GetValue(objSource, null);
                        if (objPropertyValue == null) {
                            prop.SetValue(target, null, null);
                        }
                    }
                }
            }
            return target;
        }

        /// <summary>
        /// Backup and Revert Data
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="srcContext"></param>
        /// <param name="desContext"></param>
        /// <param name="entities"></param>
        /// <param name="deepCloneEntity"></param>
        /// <returns></returns>
        //public static bool BackupRevertDataToRollbackContext<TContext, TEntity>(this TContext srcContext, TContext desContext, List<EntityWrapper<TEntity>> entities = null, bool deepCloneEntity = true)
        //    where TContext : Microsoft.EntityFrameworkCore.DbContext
        //    where TEntity : class
        //{
        //    if (srcContext == null || desContext == null) return false;
        //    const string fieldId = "Id";
        //    List<TEntity> reverstInserts = new List<TEntity>(), reverstUpdates = new List<TEntity>(), reverstDeletes = new List<TEntity>();
        //    if (entities == null) // get data on source dbcontext
        //    {
        //        List<TEntity> rollbackEntities = new List<TEntity>();
        //        var changeEntities = srcContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged && x.Entity.GetType() == typeof(TEntity)).ToList();
        //        var (inserts, updates, deletes) = (
        //            changeEntities.Where(x => x.State == EntityState.Added).OfType<TEntity>().ToList(),
        //            changeEntities.Where(x => x.State == EntityState.Modified).OfType<TEntity>().ToList(),
        //            changeEntities.Where(x => x.State == EntityState.Deleted).OfType<TEntity>().ToList()
        //            );
        //        var idQueries = updates.Select(x => (Guid)x.GetValueIgnoreCase(fieldId)).Union(deletes.Select(x => (Guid)x.GetValueIgnoreCase(fieldId))).ToList();
        //        if (idQueries.Any()) {
        //            rollbackEntities = desContext.Set<TEntity>().Where(x => idQueries.Contains(EF.Property<Guid>(x, fieldId))).AsNoTracking().ToList();
        //        }
        //        if (inserts.Any()) {
        //            reverstDeletes.AddRange(deepCloneEntity ? inserts.DeepCloneEntity() : inserts);
        //        }
        //        if (updates.Any()) {
        //            var updateIds = updates.Select(x => (Guid)x.GetValueIgnoreCase(fieldId)).Distinct().ToList();
        //            var originalUpdateData = rollbackEntities.Where(x => updateIds.Contains((Guid)x.GetValueIgnoreCase(fieldId))).ToList();
        //            if (originalUpdateData.Any()) {
        //                reverstUpdates.AddRange(deepCloneEntity ? originalUpdateData.DeepCloneEntity() : originalUpdateData);
        //            }
        //        }
        //        if (deletes.Any()) {
        //            var deleteIds = deletes.Select(x => (Guid)x.GetValueIgnoreCase(fieldId)).Distinct().ToList();
        //            var rollbackDelete = rollbackEntities.Where(x => deleteIds.Contains((Guid)x.GetValueIgnoreCase(fieldId))).ToList();
        //            if (rollbackDelete.Any()) {
        //                reverstInserts.AddRange(deepCloneEntity ? rollbackDelete.DeepCloneEntity() : rollbackDelete);
        //            }
        //        }
        //    }
        //    else {
        //        var (inserts, updates, deletes) = (
        //            entities.Where(x => x.ChangeType == EntityChangeTypeEnum.Added).Select(x => x.Value).ToList(),
        //            entities.Where(x => x.ChangeType == EntityChangeTypeEnum.Updated).Select(x => x.Value).ToList(),
        //            entities.Where(x => x.ChangeType == EntityChangeTypeEnum.Deleted).Select(x => x.Value).ToList()
        //            );

        //        if (!inserts.IsNullOrEmpty()) // insert
        //        {
        //            var items = deepCloneEntity ? inserts.DeepCloneEntity() : inserts;
        //            reverstDeletes.AddRange(items);
        //        }
        //        if (!updates.IsNullOrEmpty())  // update
        //        {
        //            var items = deepCloneEntity ? updates.DeepCloneEntity() : updates;
        //            reverstUpdates.AddRange(items);
        //        }
        //        if (!deletes.IsNullOrEmpty()) // delete
        //        {
        //            var items = deepCloneEntity ? deletes.DeepCloneEntity() : deletes;
        //            reverstInserts.AddRange(items);
        //        }
        //    }
        //    desContext.ChangeTracker.AutoDetectChangesEnabled = false;
        //    if (reverstInserts.Any()) {
        //        desContext.AddRange(reverstInserts);
        //    }
        //    if (reverstUpdates.Any()) {
        //        desContext.UpdateRange(reverstUpdates);
        //    }
        //    if (reverstDeletes.Any()) {
        //        desContext.RemoveRange(reverstDeletes);
        //    }
        //    desContext.ChangeTracker.AutoDetectChangesEnabled = true;

        //    return true;
        //}

        /// <summary>
        /// new entity wrapper from entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        //public static EntityWrapper<TEntity> ToEntityWrapper<TEntity>(this TEntity entity, Enums.EntityChangeTypeEnum type) where TEntity : class
        //{
        //    return new EntityWrapper<TEntity> { Value = entity, ChangeType = type };
        //}

        //public static IServiceCollection AddDbContextMSIAuthentication<TContext>(this IServiceCollection serviceCollection, string databaseName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
        //    where TContext : Microsoft.EntityFrameworkCore.DbContext
        //{
        //    return serviceCollection.AddDbContext<TContext, TContext>(options =>
        //    {
        //        SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI, new CustomAzureSQLAuthProvider());
        //        options.UseInMemoryDatabase(databaseName: databaseName);
        //    }, contextLifetime, optionsLifetime);
        //}

        //        public static void UseSqlServerMSI<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, string connectionString, IConfiguration configuration = null) where TContext : DbContext
        //        {
        //#if !LOCAL
        //            SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI, new CustomAzureSQLAuthProvider());
        //            var sqlConnection = new SqlConnection(connectionString);
        //            optionsBuilder.UseSqlServer(sqlConnection, options =>
        //            {
        //                options.EnableRetryOnFailure(3);
        //                options.UseCompatibilityLevel(120);
        //            });
        //#else
        //            optionsBuilder.UseSqlServer(connectionString, options => 
        //            {   
        //                options.EnableRetryOnFailure(3);
        //                options.UseCompatibilityLevel(120);
        //            });
        //#endif
        //        }

        //        public static void UseSqlServerMSI(this DbContextOptionsBuilder optionsBuilder, string connectionString, IConfiguration configuration = null, int? commandTimeout = null)
        //        {
        //#if !LOCAL
        //            SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryMSI, new CustomAzureSQLAuthProvider());
        //            var sqlConnection = new SqlConnection(connectionString);
        //            optionsBuilder.UseSqlServer(sqlConnection, options =>
        //            {
        //                options.EnableRetryOnFailure(3);
        //                options.UseCompatibilityLevel(120);
        //                if (commandTimeout.HasValue) {
        //                    options.CommandTimeout(commandTimeout.Value);
        //                }
        //            });
        //#else
        //            optionsBuilder.UseSqlServer(connectionString, options => 
        //            {   
        //                options.EnableRetryOnFailure(3);
        //                options.UseCompatibilityLevel(120);
        //            });
        //#endif
        //        }

        /// <summary>
        /// Get detail add/update/delete entities
        /// </summary>
        /// <returns></returns>
        public static (List<TEntity>, List<TEntity>, List<TEntity>) GetAddUpdateDeleteEntries<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            var entities = dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged && x.Entity.GetType().Name == typeof(TEntity).Name);
            return (
                entities.Where(x => x.State == EntityState.Added).Select(x => x.Entity).Cast<TEntity>().ToList(),
                entities.Where(x => x.State == EntityState.Modified).Select(x => x.Entity).Cast<TEntity>().ToList(),
                entities.Where(x => x.State == EntityState.Deleted).Select(x => x.Entity).Cast<TEntity>().ToList()
                );
        }

        /// Get entity from change tracker
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static (List<TEntity>, List<TEntity>, List<TEntity>) SplitAddUpdateDeleteEntries<TEntity>(this Dictionary<Type, (List<object>, List<object>, List<object>)> entities)
        {
            var (insert, update, delete) = entities.GetValueOrDefault(typeof(TEntity));
            insert ??= new List<object>();
            update ??= new List<object>();
            delete ??= new List<object>();
            return (insert.OfType<TEntity>().ToList(), update.OfType<TEntity>().ToList(), delete.OfType<TEntity>().ToList());
        }

        /// Get entity from change tracker
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static (List<TEntity>, List<TEntity>) SplitAddUpdateEntries<TEntity>(this Dictionary<Type, (List<object>, List<object>, List<object>)> entities)
        {
            var (insert, update, delete) = entities.GetValueOrDefault(typeof(TEntity));
            insert ??= new List<object>();
            update ??= new List<object>();
            delete ??= new List<object>();
            return (insert.OfType<TEntity>().ToList(), update.OfType<TEntity>().ToList());
        }

        /// <summary>
        /// Get list data need to update from change tracker into dictionary
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entityState"></param>
        /// <returns></returns>
        public static Dictionary<Type, (List<object>, List<object>, List<object>)> GetDictionaryDataModifiedFromTracker(this Microsoft.EntityFrameworkCore.DbContext dbContext, params EntityState[] entityState)
        {
            return dbContext.ChangeTracker.Entries()
                                          .Where(x => entityState.Contains(x.State))
                                          .GroupBy(x => x.Entity.GetType())
                                          .ToDictionary(x => x.Key, x => SplitEntity(x.ToList()));
        }

        /// <summary>
        /// Split entities to group list
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        private static (List<object>, List<object>, List<object>) SplitEntity(List<EntityEntry> entries)
        {
            var listInsert = entries.Where(x => x.State == EntityState.Added).Select(x => x.Entity).ToList();
            var listUpdate = entries.Where(x => x.State == EntityState.Modified).Select(x => x.Entity).ToList();
            var listDelete = entries.Where(x => x.State == EntityState.Deleted).Select(x => x.Entity).ToList();
            return (listInsert, listUpdate, listDelete);
        }

        /// <summary>
        /// Detach entities
        /// </summary>
        /// <param name="entityStates"></param>
        public static void DetachAllEntities(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<string> detachEntities)
        {
            var changedEntriesCopy = dbContext.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Unchanged && detachEntities.Contains(e.Entity.GetType().Name))
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        /// <summary>
        /// Detach entities
        /// </summary>
        /// <param name="entityStates"></param>
        public static void DetachAllEntitiesByIds<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> detachEntitieIds)
        {
            var changedEntriesCopy = dbContext.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Unchanged
                            && e.Entity.GetType().Name == typeof(TEntity).Name
                            && detachEntitieIds.Contains(((Guid?)e.Entity.GetValueIgnoreCase("Id")).GetValueOrDefault()))
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        /// <summary>
        /// Create temp table using EF
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableTemp"></param>
        /// <param name="listWhereInIds"></param>
        /// <returns></returns>
        public static async Task<bool> CreateTempTableAsync(this Microsoft.EntityFrameworkCore.DbContext dbContext, string tableTemp, List<Guid> listWhereInIds)
        {
            bool result = false;
            if (listWhereInIds.IsNullOrEmpty()) return await Task.FromResult(result);

            var simpleLookups = new List<Common.TempTableData>(listWhereInIds.Distinct().Select(x => new Common.TempTableData { Id = x }).ToList());
            var columns = new List<string> { nameof(Common.TempTableData.Id) };
            try {
                SqlConnection sqlCon = (SqlConnection)dbContext.Database.GetDbConnection();
                var sqlCommand = $@"IF OBJECT_ID(N'tempdb..{tableTemp}') IS NOT NULL DROP TABLE {tableTemp} ; 
                                    CREATE TABLE {tableTemp} (id uniqueidentifier, PRIMARY KEY (id))";

                if (sqlCon.State != ConnectionState.Open) {
                    await dbContext.Database.OpenConnectionAsync();
                }

                await dbContext.Database.ExecuteSqlRawAsync(sqlCommand);
                using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlCon)) {
                    bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                    bulkcopy.DestinationTableName = tableTemp;
                    using (var dataReader = new ListDataReader<Common.TempTableData>(simpleLookups, columns.ToArray())) {
                        await bulkcopy.WriteToServerAsync(dataReader);
                        bulkcopy.Close();
                    }
                }
                result = true;
            }
            catch (Exception) {
                await dbContext.Database.CloseConnectionAsync();
                throw;
            }

            return true;
        }

        /// <summary>
        /// Create temp table using EF for string type
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableTemp"></param>
        /// <param name="listWhereInStringValues"></param>
        /// <returns></returns>
        public static async Task<bool> CreateTempTableWithStringTypeAsync(this Microsoft.EntityFrameworkCore.DbContext dbContext, string tableTemp, List<string> listWhereInStringValues)
        {
            bool result = false;
            if (listWhereInStringValues.IsNullOrEmpty()) return await Task.FromResult(result);

            var maxLeng = listWhereInStringValues.Max(x => x.Length);
            var simpleLookups = new List<Common.TempTableDataString>(listWhereInStringValues.Distinct().Select(x => new Common.TempTableDataString { Id = x }).ToList());
            var columns = new List<string> { nameof(Common.TempTableDataString.Id) };
            try {
                SqlConnection sqlCon = (SqlConnection)dbContext.Database.GetDbConnection();
                var sqlCommand = $@"IF OBJECT_ID(N'tempdb..{tableTemp}') IS NOT NULL DROP TABLE {tableTemp} ; 
                                    CREATE TABLE {tableTemp} (id nvarchar({maxLeng}), PRIMARY KEY (id))";

                if (sqlCon.State != ConnectionState.Open) {
                    await dbContext.Database.OpenConnectionAsync();
                }

                await dbContext.Database.ExecuteSqlRawAsync(sqlCommand);
                using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlCon)) {
                    bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                    bulkcopy.DestinationTableName = tableTemp;
                    using (var dataReader = new ListDataReader<Common.TempTableDataString>(simpleLookups, columns.ToArray())) {
                        await bulkcopy.WriteToServerAsync(dataReader);
                        bulkcopy.Close();
                    }
                }
                result = true;
            }
            catch (Exception) {
                await dbContext.Database.CloseConnectionAsync();
                throw;
            }

            return true;
        }

        /// <summary>
        /// Create temp table using EF
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableTemp"></param>
        /// <param name="listWhereInIds"></param>
        /// <returns></returns>
        public static bool CreateTempTable(this Microsoft.EntityFrameworkCore.DbContext dbContext, string tableTemp, List<Guid> listWhereInIds)
        {
            bool result = false;
            if (listWhereInIds.IsNullOrEmpty()) return result;

            var simpleLookups = new List<Common.TempTableData>(listWhereInIds.Distinct().Select(x => new Common.TempTableData { Id = x }).ToList());
            var columns = new List<string> { nameof(Common.TempTableData.Id) };
            try {
                SqlConnection sqlCon = (SqlConnection)dbContext.Database.GetDbConnection();
                var sqlCommand = $@"IF OBJECT_ID(N'tempdb..{tableTemp}') IS NOT NULL DROP TABLE {tableTemp} ; 
                                    CREATE TABLE {tableTemp} (id uniqueidentifier, PRIMARY KEY (id))";

                if (sqlCon.State != ConnectionState.Open) {
                    dbContext.Database.OpenConnection();
                }
                dbContext.Database.ExecuteSqlRaw(sqlCommand);
                using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlCon)) {
                    bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                    bulkcopy.DestinationTableName = tableTemp;
                    using (var dataReader = new ListDataReader<Common.TempTableData>(simpleLookups, columns.ToArray())) {
                        bulkcopy.WriteToServer(dataReader);
                        bulkcopy.Close();
                    }
                }
                result = true;
            }
            catch (Exception) {
                dbContext.Database.CloseConnection();
                throw;
            }

            return true;
        }

        /// <summary>
        /// Delete temp table from EF
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableTemp"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteTempTableAsync(this Microsoft.EntityFrameworkCore.DbContext dbContext, string tableTemp)
        {
            bool result = false;
            try {
                SqlConnection sqlCon = (SqlConnection)dbContext.Database.GetDbConnection();
                var sqlCommand = $"IF OBJECT_ID(N'tempdb..{tableTemp}') IS NOT NULL DROP TABLE {tableTemp}";
                if (sqlCon.State != ConnectionState.Open) {
                    await dbContext.Database.OpenConnectionAsync();
                }

                await dbContext.Database.ExecuteSqlRawAsync(sqlCommand);
                if (sqlCon.State == ConnectionState.Open) {
                    await dbContext.Database.CloseConnectionAsync();
                }
                result = true;
            }
            catch (Exception) {
                await dbContext.Database.CloseConnectionAsync();
                throw;
            }

            return result;
        }

        /// <summary>
        /// Delete temp table from EF
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableTemp"></param>
        /// <param name="schemas"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteTempTableAsync(this Microsoft.EntityFrameworkCore.DbContext dbContext, string tableTemp, string schemas)
        {
            bool result = false;
            try {
                SqlConnection sqlCon = (SqlConnection)dbContext.Database.GetDbConnection();
                var sqlCommand = $"IF OBJECT_ID(N'{schemas}.{tableTemp}') IS NOT NULL DROP TABLE {schemas}.{tableTemp}";
                if (sqlCon.State != ConnectionState.Open) {
                    await dbContext.Database.OpenConnectionAsync();
                }

                await dbContext.Database.ExecuteSqlRawAsync(sqlCommand);
                result = true;
            }
            catch (Exception) {
                await dbContext.Database.CloseConnectionAsync();
                throw;
            }

            return result;
        }

        /// <summary>
        /// Delete temp table from EF
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableTemp"></param>
        /// <returns></returns>
        public static bool DeleteTempTable(this Microsoft.EntityFrameworkCore.DbContext dbContext, string tableTemp)
        {
            bool result = false;
            try {
                SqlConnection sqlCon = (SqlConnection)dbContext.Database.GetDbConnection();
                var sqlCommand = $"IF OBJECT_ID(N'tempdb..{tableTemp}') IS NOT NULL DROP TABLE {tableTemp}";
                if (sqlCon.State != ConnectionState.Open) {
                    dbContext.Database.OpenConnection();
                }
                dbContext.Database.ExecuteSqlRaw(sqlCommand);
                if (sqlCon.State == ConnectionState.Open) {
                    dbContext.Database.CloseConnection();
                }
                result = true;
            }
            catch (Exception) {
                dbContext.Database.CloseConnection();
                throw;
            }

            return result;
        }

        /// <summary>
        /// GetLargeWhereInSqlTempTableAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="func"></param>
        /// <param name="maxWhereIn"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> GetLargeWhereInSqlTempTableAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, Func<bool, List<TEntity>> func, int maxWhereIn)
        {
            var result = new List<TEntity>();

            if (listWhereInIds.IsNullOrEmpty())
                return await Task.FromResult(result);

            bool useTempTable = false;
            if (listWhereInIds.Count >= maxWhereIn) {
                await dbContext.CreateTempTableAsync($"#{nameof(Common.TempTableData)}", listWhereInIds);
                useTempTable = true;
                result = func.Invoke(useTempTable);
                await dbContext.DeleteTempTableAsync($"#{nameof(Common.TempTableData)}");
            }
            else {
                result = func.Invoke(useTempTable);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// GetLargeWhereInSqlTempTableAsync with string type
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInStringValues"></param>
        /// <param name="func"></param>
        /// <param name="maxWhereIn"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> GetLargeWhereInSqlTempTableStringTypeAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<string> listWhereInStringValues, Func<bool, Task<List<TEntity>>> func, int maxWhereIn = CommonConstants.MinimumIdsForCreateTempTable)
        {
            var result = new List<TEntity>();

            if (listWhereInStringValues.IsNullOrEmpty())
                return await Task.FromResult(result);

            bool useTempTable = false;
            if (listWhereInStringValues.Count >= maxWhereIn) {
                await dbContext.CreateTempTableWithStringTypeAsync($"#{nameof(Common.TempTableDataString)}", listWhereInStringValues);
                useTempTable = true;
                result = await func.Invoke(useTempTable);
                await dbContext.DeleteTempTableAsync($"#{nameof(Common.TempTableDataString)}");
            }
            else {
                result = await func.Invoke(useTempTable);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// GetLargeWhereInSqlTempTableAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInTempDatas"></param>
        /// <param name="func"></param>
        /// <param name="maxWhereIn"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> GetLargeWhereInSqlTempTableAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<TempTableDataForGuidIdAndIntValue> listWhereInTempDatas, Func<Task<List<TEntity>>> func)
        {
            var result = new List<TEntity>();

            if (listWhereInTempDatas.IsNullOrEmpty())
                return await Task.FromResult(result);

            await dbContext.CreateTempTableAsync($"#{nameof(Common.TempTableDataForGuidIdAndIntValue)}", listWhereInTempDatas);

            result = await func.Invoke();

            await dbContext.DeleteTempTableAsync($"#{nameof(Common.TempTableDataForGuidIdAndIntValue)}");

            return result;
        }

        /// <summary>
        /// Create temp table using EF
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableTemp"></param>
        /// <param name="listWhereInIds"></param>
        /// <returns></returns>
        public static async Task<bool> CreateTempTableAsync(this Microsoft.EntityFrameworkCore.DbContext dbContext, string tableTemp, List<TempTableDataForGuidIdAndIntValue> listWhereInIds)
        {
            bool result = false;
            if (listWhereInIds.IsNullOrEmpty()) return await Task.FromResult(result);

            var simpleLookups = new List<Common.TempTableDataForGuidIdAndIntValue>(listWhereInIds.Distinct().Select(x => new Common.TempTableDataForGuidIdAndIntValue { GuidId = x.GuidId, IntValue = x.IntValue }).ToList());
            var columns = new List<string>
            {
                nameof(Common.TempTableDataForGuidIdAndIntValue.GuidId),
                nameof(Common.TempTableDataForGuidIdAndIntValue.IntValue)
            };

            try {
                SqlConnection sqlCon = (SqlConnection)dbContext.Database.GetDbConnection();
                var sqlCommand = $@"IF OBJECT_ID(N'tempdb..{tableTemp}') IS NOT NULL DROP TABLE {tableTemp} ; 
                                    CREATE TABLE {tableTemp} ({nameof(Common.TempTableDataForGuidIdAndIntValue.GuidId)} uniqueidentifier, {nameof(Common.TempTableDataForGuidIdAndIntValue.IntValue)} int)";

                if (sqlCon.State != ConnectionState.Open) {
                    await dbContext.Database.OpenConnectionAsync();
                }

                await dbContext.Database.ExecuteSqlRawAsync(sqlCommand);
                using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlCon)) {
                    bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                    bulkcopy.DestinationTableName = tableTemp;
                    using (var dataReader = new ListDataReader<Common.TempTableDataForGuidIdAndIntValue>(simpleLookups, columns.ToArray())) {
                        await bulkcopy.WriteToServerAsync(dataReader);
                        bulkcopy.Close();
                    }
                }
                result = true;
            }
            catch (Exception) {
                await dbContext.Database.CloseConnectionAsync();
                throw;
            }

            return true;
        }

        /// <summary>
        /// Paging List Entities
        /// Caution isOrderedById, pass isOrderedById:true when query ordered by != Id will return wrong result. 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="isOrderedById"></param>
        /// <param name="batchRead"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> AsPageOrderedListEntitiesAsync<TEntity>(this IQueryable<TEntity> query,
            bool isOrderedById = false,
            int batchRead = CommonConstants.LimitDataReader)
        {
            List<TEntity> result = new List<TEntity>();

            var totalRows = query.Count();

            // Get the total of pages that need paging
            var totalPages = Math.Ceiling((decimal)totalRows / batchRead);

            // In case the query has ordered by != Id or no need for paging
            if (!isOrderedById || totalPages <= 1) {
                for (int page = 0; page < totalPages; page++) {
                    var items = await query.Skip(page * batchRead).Take(batchRead).ToListAsync();
                    result.AddRange(items);
                }

                return result;
            }

            // In case the query has ordered by Id and need for paging
            object lastIdValue = null;
            bool isGuidId = typeof(IGuidKeyEntity).GetTypeInfo().IsAssignableFrom(typeof(TEntity).Ge‌​tTypeInfo());
            bool isIntId = typeof(IIntKeyEntity).GetTypeInfo().IsAssignableFrom(typeof(TEntity).Ge‌​tTypeInfo());

            for (int page = 0; page < totalPages; page++) {
                List<TEntity> items = new List<TEntity>();
                if (lastIdValue != null && isGuidId) {
                    items = await query.Where(x => ((IGuidKeyEntity)x).Id.CompareTo((Guid)lastIdValue) > 0).Take(batchRead).ToListAsync();
                }
                else if (lastIdValue != null && isIntId) {
                    items = await query.Where(x => ((IIntKeyEntity)x).Id > (int)lastIdValue).Take(batchRead).ToListAsync();
                }
                else {
                    items = await query.Skip(page * batchRead).Take(batchRead).ToListAsync();
                }

                if ((page < totalPages - 1) && (isGuidId || isIntId)) {
                    var lastItem = items.LastOrDefault();
                    if (lastItem != null) {
                        lastIdValue = typeof(TEntity).GetProperty("Id")?.GetValue(lastItem, null);
                    }
                    else
                        lastIdValue = null;
                }

                result.AddRange(items);
            }

            return result;
        }

        /// <summary>
        /// Paging list
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="totalRows"></param>
        /// <param name="batchRead"></param>
        /// <param name="selector"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        private static async Task<List<TEntity>> AsPageOrderedListAsync<TEntity>(this IQueryable<TEntity> query,
            int totalRows,
            int batchRead = CommonConstants.LimitDataReader,
            Expression<Func<TEntity, TEntity>> selector = null,
            Expression<Func<TEntity, object>> orderBy = null)
        {
            const string fieldId = "Id";
            var result = new List<TEntity>();

            // Get the total of pages that need paging
            var totalPages = Math.Ceiling((decimal)totalRows / batchRead);

            // In case the query has ordered by != Id or no need for paging
            if (orderBy != null || totalPages <= 1) {
                if (orderBy != null) {
                    query = query.OrderBy(orderBy);
                }

                if (selector != null) {
                    query = query.Select(selector);
                }

                for (int page = 0; page < totalPages; page++) {
                    var items = await query.Skip(page * batchRead).Take(batchRead).ToListAsync();
                    result.AddRange(items);
                }

                return result;
            }

            // Add order by Id
            query = query.OrderBy(p => EF.Property<object>(p, fieldId));

            // Add selector and check Id must existing in selector
            if (selector != null) {
                var parameter = Expression.Parameter(typeof(TEntity), fieldId);
                var members = new string[] { fieldId };
                var bindings = members
                    .Select(name => Expression.PropertyOrField(parameter, name))
                    .Select(member => Expression.Bind(member.Member, member));

                var body = Expression.MemberInit(Expression.New(typeof(TEntity)), bindings);
                var idSelector = Expression.Lambda<Func<TEntity, TEntity>>(body, parameter);
                var combineIdSelector = ExpressionExtensions.Combine(selector, idSelector);
                if (combineIdSelector != null) {
                    query = query.Select(combineIdSelector);
                }
            }

            // In case the query has ordered by Id and need for paging
            object lastIdValue = null;
            bool isGuidId = typeof(IGuidKeyEntity).GetTypeInfo().IsAssignableFrom(typeof(TEntity).Ge‌​tTypeInfo());
            bool isIntId = typeof(IIntKeyEntity).GetTypeInfo().IsAssignableFrom(typeof(TEntity).Ge‌​tTypeInfo());

            for (int page = 0; page < totalPages; page++) {
                List<TEntity> items = new List<TEntity>();
                if (lastIdValue != null && isGuidId) {
                    items = await query.Where(x => ((IGuidKeyEntity)x).Id.CompareTo((Guid)lastIdValue) > 0).Take(batchRead).ToListAsync();
                }
                else if (lastIdValue != null && isIntId) {
                    items = await query.Where(x => ((IIntKeyEntity)x).Id > (int)lastIdValue).Take(batchRead).ToListAsync();
                }
                else {
                    items = await query.Skip(page * batchRead).Take(batchRead).ToListAsync();
                }

                if ((page < totalPages - 1) && (isGuidId || isIntId)) {
                    var lastItem = items.LastOrDefault();
                    if (lastItem != null) {
                        lastIdValue = typeof(TEntity).GetProperty("Id")?.GetValue(lastItem, null);
                    }
                    else
                        lastIdValue = null;
                }

                result.AddRange(items);
            }

            return result;
        }

        /// <summary>
        /// Where bulk contains with temp table
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="tempTable"></param>
        /// <param name="keyContains"></param>
        /// <param name="selector"></param>
        /// <param name="orderBy"></param>
        /// <param name="isTracking"></param>
        /// <param name="batchRead"></param>
        /// <param name="extraCondition"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> WhereBulkContainsAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, IQueryable<Guid> tempTable, string keyContains, Expression<Func<TEntity, TEntity>>? selector = null, Expression<Func<TEntity, object>>? orderBy = null, bool isTracking = false, int batchRead = CommonConstants.LimitDataReader, Expression<Func<TEntity, bool>>? extraCondition = null, int shardNumber = 0, params Expression<Func<TEntity, Object>>[] includes)
            where TEntity : class
        {
            var result = new List<TEntity>();

            if (listWhereInIds.IsNullOrEmpty())
                return await Task.FromResult(result);

            var query = isTracking ? dbContext.Set<TEntity>() : dbContext.Set<TEntity>().WithShardNumber(dbContext, shardNumber).AsNoTracking();

            if (includes != null && includes.Length > 0) {
                foreach (var include in includes) {
                    query = query.Include(include);
                }
            }

            var useTempTable = listWhereInIds.Count > CommonConstants.MinimumIdsForCreateTempTable;
            var collectionIds = useTempTable ? tempTable : listWhereInIds.AsEnumerable();

            query = query.Where(p => collectionIds.Contains(EF.Property<Guid>(p, keyContains)));

            if (extraCondition != null) {
                query = query.Where(extraCondition);
            }

            if (useTempTable) {
                await dbContext.CreateTempTableAsync($"#{nameof(Common.TempTableData)}", listWhereInIds);
            }

            var totalRows = await query.CountAsync();
            result = await query.AsPageOrderedListAsync(totalRows, batchRead, selector, orderBy);

            if (useTempTable) {
                await dbContext.DeleteTempTableAsync($"#{nameof(Common.TempTableData)}");
            }
            return result;
        }

        /// <summary>
        /// Where bulk contains with temp table
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="tempTable"></param>
        /// <param name="keyContains"></param>
        /// <param name="selector"></param>
        /// <param name="orderBy"></param>
        /// <param name="isTracking"></param>
        /// <param name="batchRead"></param>
        /// <param name="extraCondition"></param>
        /// <returns></returns>
        public static List<TEntity> WhereBulkContains<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, IQueryable<Guid> tempTable, string keyContains, Expression<Func<TEntity, TEntity>>? selector = null, Expression<Func<TEntity, object>>? orderBy = null, bool isTracking = false, int batchRead = CommonConstants.LimitDataReader, Expression<Func<TEntity, bool>>? extraCondition = null, params Expression<Func<TEntity, Object>>[] includes)
            where TEntity : class
        {
            var result = new List<TEntity>();

            if (listWhereInIds.IsNullOrEmpty())
                return result;

            var query = isTracking ? dbContext.Set<TEntity>() : dbContext.Set<TEntity>().AsNoTracking();

            if (includes != null && includes.Length > 0) {
                foreach (var include in includes) {
                    query = query.Include(include);
                }
            }

            var useTempTable = listWhereInIds.Count > CommonConstants.MinimumIdsForCreateTempTable;
            var collectionIds = useTempTable ? tempTable : listWhereInIds.AsEnumerable();

            query = query.Where(p => collectionIds.Contains(EF.Property<Guid>(p, keyContains)));

            if (extraCondition != null) {
                query = query.Where(extraCondition);
            }

            if (useTempTable) {
                dbContext.CreateTempTable($"#{nameof(Common.TempTableData)}", listWhereInIds);
            }

            var totalRows = query.Count();
            result = query.AsPageOrderedListAsync(totalRows, batchRead, selector, orderBy).Result;

            if (useTempTable) {
                dbContext.DeleteTempTable($"#{nameof(Common.TempTableData)}");
            }

            return result;
        }

        /// <summary>
        /// Where bulk contains with temp table not use paging
        /// Code Clone from WhereBulkContains which support get data without apply paging and return custom model
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="tempTable"></param>
        /// <param name="keyContains"></param>
        /// <param name="selector"></param>
        /// <param name="orderBy"></param>
        /// <param name="isTracking"></param>
        /// <param name="extraCondition"></param>
        /// <returns></returns>
        public static List<TResult> WhereBulkContainsNoPaging<TEntity, TResult>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, IQueryable<Guid> tempTable, string keyContains, Expression<Func<TEntity, TResult>> selector, Expression<Func<TEntity, object>>? orderBy = null, bool isTracking = false, Expression<Func<TEntity, bool>>? extraCondition = null, params Expression<Func<TEntity, Object>>[] includes)
            where TEntity : class
        {
            var result = new List<TResult>();

            if (listWhereInIds.IsNullOrEmpty())
                return result;

            var query = isTracking ? dbContext.Set<TEntity>() : dbContext.Set<TEntity>().AsNoTracking();

            if (includes != null && includes.Length > 0) {
                foreach (var include in includes) {
                    query = query.Include(include);
                }
            }

            var useTempTable = listWhereInIds.Count > CommonConstants.MinimumIdsForCreateTempTable;
            var collectionIds = useTempTable ? tempTable : listWhereInIds.AsEnumerable();

            query = query.Where(p => collectionIds.Contains(EF.Property<Guid>(p, keyContains)));

            if (extraCondition != null) {
                query = query.Where(extraCondition);
            }

            if (useTempTable) {
                dbContext.CreateTempTable($"#{nameof(Common.TempTableData)}", listWhereInIds);
            }

            if (orderBy != null) {
                query = query.OrderBy(orderBy);
            }

            result = query.Select(selector).ToList();

            if (useTempTable) {
                dbContext.DeleteTempTable($"#{nameof(Common.TempTableData)}");
            }

            return result;
        }

        /// <summary>
        /// Where Not bulk contains with temp table
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="tempTable"></param>
        /// <param name="keyContains"></param>
        /// <param name="selector"></param>
        /// <param name="orderBy"></param>
        /// <param name="isTracking"></param>
        /// <param name="batchRead"></param>
        /// <param name="extraCondition"></param>
        /// <returns></returns>
        public static List<TEntity> WhereNotBulkContains<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, IQueryable<Guid> tempTable, string keyContains, Expression<Func<TEntity, TEntity>>? selector = null, Expression<Func<TEntity, object>>? orderBy = null, bool isTracking = false, int batchRead = CommonConstants.LimitDataReader, Expression<Func<TEntity, bool>>? extraCondition = null, params Expression<Func<TEntity, Object>>[] includes)
            where TEntity : class
        {
            var result = new List<TEntity>();

            if (listWhereInIds.IsNullOrEmpty())
                return result;

            var query = isTracking ? dbContext.Set<TEntity>() : dbContext.Set<TEntity>().AsNoTracking();

            if (includes != null && includes.Length > 0) {
                foreach (var include in includes) {
                    query = query.Include(include);
                }
            }

            var useTempTable = listWhereInIds.Count > CommonConstants.MinimumIdsForCreateTempTable;
            var collectionIds = useTempTable ? tempTable : listWhereInIds.AsEnumerable();

            query = query.Where(p => !collectionIds.Contains(EF.Property<Guid>(p, keyContains)));

            if (extraCondition != null) {
                query = query.Where(extraCondition);
            }

            if (useTempTable) {
                dbContext.CreateTempTable($"#{nameof(Common.TempTableData)}", listWhereInIds);
            }

            var count = query.Count();

            if (orderBy != null) {
                query = query.OrderBy(orderBy);
            }
            else if (count > batchRead) {
                query = query.OrderBy(p => EF.Property<object>(p, "Id"));
            }

            if (selector != null) {
                query = query.Select(selector);
            }

            var pageSize = batchRead;
            var totalPage = Math.Ceiling((decimal)count / pageSize);
            for (int page = 0; page < totalPage; page++) {
                var items = query.Skip(page * pageSize).Take(pageSize).ToList();
                result.AddRange(items);
            }
            if (useTempTable) {
                dbContext.DeleteTempTable($"#{nameof(Common.TempTableData)}");
            }
            return result;
        }

        /// <summary>
        /// Where not contains
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="tempTable"></param>
        /// <param name="keyContains"></param>
        /// <param name="selector"></param>
        /// <param name="orderBy"></param>
        /// <param name="isTracking"></param>
        /// <param name="batchRead"></param>
        /// <param name="extraCondition"></param>
        /// <param name="isStringType"> true: if keyContains's type is string else it's false</param>
        /// <returns></returns>
        public static async Task<List<TEntity>> WhereNotBulkContainsAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, IQueryable<Guid> tempTable, string keyContains, Expression<Func<TEntity, TEntity>>? selector = null, Expression<Func<TEntity, object>>? orderBy = null, bool isTracking = false, int batchRead = CommonConstants.LimitDataReader, Expression<Func<TEntity, bool>>? extraCondition = null, bool isStringType = false, params Expression<Func<TEntity, Object>>[] includes)
            where TEntity : class
        {
            var result = new List<TEntity>();

            if (listWhereInIds.IsNullOrEmpty())
                return await Task.FromResult(result);

            var query = isTracking ? dbContext.Set<TEntity>() : dbContext.Set<TEntity>().AsNoTracking();

            if (includes != null && includes.Length > 0) {
                foreach (var include in includes) {
                    query = query.Include(include);
                }
            }

            var useTempTable = listWhereInIds.Count > CommonConstants.MinimumIdsForCreateTempTable;
            var collectionIds = useTempTable ? tempTable : listWhereInIds.AsEnumerable();

            if (isStringType) {
                var ids = collectionIds.Select(i => i.ToString().ToLower());
                query = query.Where(p => !ids.Contains(EF.Property<string>(p, keyContains)));
            }
            else {
                query = query.Where(p => !collectionIds.Contains(EF.Property<Guid>(p, keyContains)));
            }

            if (extraCondition != null) {
                query = query.Where(extraCondition);
            }

            if (useTempTable) {
                await dbContext.CreateTempTableAsync($"#{nameof(Common.TempTableData)}", listWhereInIds);
            }

            var count = await query.CountAsync();

            if (orderBy != null) {
                query = query.OrderBy(orderBy);
            }
            else if (count > batchRead) {
                query = query.OrderBy(p => EF.Property<object>(p, "Id"));
            }

            if (selector != null) {
                query = query.Select(selector);
            }

            var pageSize = batchRead;
            var totalPage = Math.Ceiling((decimal)count / pageSize);
            for (int page = 0; page < totalPage; page++) {
                var items = query.Skip(page * pageSize).Take(pageSize).ToList();
                result.AddRange(items);
            }
            if (useTempTable) {
                await dbContext.DeleteTempTableAsync($"#{nameof(Common.TempTableData)}");
            }
            return result;
        }

        /// <summary>
        /// Excute query and return data using batch read
        /// Make sure that the list selection contains the Id property when don't use param orderBy  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="batchRead"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> AsPageListAsync<TEntity>(this IQueryable<TEntity> query, int batchRead = CommonConstants.LimitDataReader, Expression<Func<TEntity, object>>? orderBy = null, Expression<Func<TEntity, object>>? thenOrderBy = null)
        {
            var result = new List<TEntity>();
            var count = await query.CountAsync();
            var pageSize = batchRead;
            var totalPage = Math.Ceiling((decimal)count / pageSize);

            if (orderBy != null) {
                query = thenOrderBy != null ? query.OrderBy(orderBy).ThenBy(thenOrderBy) : query.OrderBy(orderBy);
            }
            else if (count > batchRead) {
                query = thenOrderBy != null ? query.OrderBy(p => EF.Property<object>(p, "Id")).ThenBy(thenOrderBy) : query.OrderBy(p => EF.Property<object>(p, "Id"));
            }

            for (int page = 0; page < totalPage; page++) {
                var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync();
                result.AddRange(items);
            }
            return result;
        }

        /// <summary>
        /// Excute query and return data using batch read
        /// Make sure that the list selection contains the Id property when don't use param orderBy  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="batchRead"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public static List<TEntity> AsPageList<TEntity>(this IQueryable<TEntity> query, int batchRead = CommonConstants.LimitDataReader, Expression<Func<TEntity, object>>? orderBy = null)
        {
            var result = new List<TEntity>();
            var count = query.Count();
            var pageSize = batchRead;
            var totalPage = Math.Ceiling((decimal)count / pageSize);

            if (orderBy != null) {
                query = query.OrderBy(orderBy);
            }
            else if (count > batchRead) {
                query = query.OrderBy(p => EF.Property<object>(p, "Id"));
            }

            for (int page = 0; page < totalPage; page++) {
                var items = query.Skip(page * pageSize).Take(pageSize).ToList();
                result.AddRange(items);
            }
            return result;
        }

        /// <summary>
        /// This method support get Id field of specific table using temp table method. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="tempTable"></param>
        /// <param name="ids"></param>
        /// <param name="keyContains"></param>
        /// <param name="batchRead"></param>
        /// <returns> the result return list guids
        /// </returns>
        public static async Task<List<Guid>> AsPageListIdsAsync<T>(this Microsoft.EntityFrameworkCore.DbContext dbContext, IQueryable<Guid> tempTable, List<Guid> ids, string keyContains, Expression<Func<T, bool>>? extraCondition = null, int batchRead = CommonConstants.LimitDataReader) where T : class
        {
            var result = new List<Guid>();
            if (ids.IsNullOrEmpty())
                return await Task.FromResult(result);
            const string fieldId = "Id";
            var parameter = Expression.Parameter(typeof(T), fieldId);
            var members = new string[] { fieldId };
            var bindings = members
                                .Select(name => Expression.PropertyOrField(parameter, name))
                                .Select(member => Expression.Bind(member.Member, member));

            var body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
            var selector = Expression.Lambda<Func<T, T>>(body, parameter);

            var targetIds = await dbContext.WhereBulkContainsAsync<T>(
                listWhereInIds: ids,
                tempTable: tempTable,
                keyContains: keyContains,
                batchRead: batchRead,
                selector: selector,
                extraCondition: extraCondition
                );
            result = targetIds.Select(x => (Guid)x.GetType().GetProperty(fieldId).GetValue(x)).ToList();
            return result;
        }

        /// <summary>
        /// GetLargeWhereInSqlTempTableAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="func"></param>
        /// <param name="maxWhereIn"></param>
        /// <returns></returns>
        public static async Task<List<TEntity>> GetLargeWhereInSqlTempTableAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, Func<bool, Task<List<TEntity>>> func, int maxWhereIn)
        {
            var result = new List<TEntity>();

            if (listWhereInIds.IsNullOrEmpty())
                return result;

            bool useTempTable = false;
            if (listWhereInIds.Count >= maxWhereIn) {
                await dbContext.CreateTempTableAsync($"#{nameof(Common.TempTableData)}", listWhereInIds);
                useTempTable = true;
                result = await func.Invoke(useTempTable);
                await dbContext.DeleteTempTableAsync($"#{nameof(Common.TempTableData)}");
            }
            else {
                result = await func.Invoke(useTempTable);
            }

            return result;
        }

        /// <summary>
		/// GetListEntityByListIds Using TempTable
        /// </summary>
        /// <param name="listObj"></param>
        /// <param name="engagementId"></param>
        /// <returns>IList<T></returns>
        public static IList<TModel> GetListEntityByListIds<TEntity, TModel>(Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listIds, string field, Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TModel>> select) where TEntity : class
        {
            List<TModel> InlineFunc(bool isUseTempTable)
            {
                var tempQueryAble = isUseTempTable ? dbContext.Set<TempTableData>().Select(x => x.Id) : listIds.AsEnumerable();
                var query = dbContext.Set<TEntity>().AsNoTracking();
                if (listIds?.Count > 0) {
                    query = query.Where(x => tempQueryAble.Contains(EF.Property<Guid>(x, field)));
                }
                if (where != null) {
                    query = query.Where(where);
                }
                return query.Select(select).ToList();
            }

            return GetLargeWhereInSqlTempTable(dbContext, listIds, InlineFunc, CommonConstants.MinimumIdsForCreateTempTable);
        }

        /// <summary>
        /// GetLargeWhereInSqlTempTableAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="listWhereInIds"></param>
        /// <param name="func"></param>
        /// <param name="maxWhereIn"></param>
        /// <returns></returns>
        public static List<TEntity> GetLargeWhereInSqlTempTable<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, List<Guid> listWhereInIds, Func<bool, List<TEntity>> func, int maxWhereIn)
        {
            var result = new List<TEntity>();

            if (listWhereInIds.IsNullOrEmpty())
                return result;

            bool useTempTable = false;
            if (listWhereInIds.Count >= maxWhereIn) {
                dbContext.CreateTempTable($"#{nameof(Common.TempTableData)}", listWhereInIds);
                useTempTable = true;
                result = func.Invoke(useTempTable);
                dbContext.DeleteTempTable($"#{nameof(Common.TempTableData)}");
            }
            else {
                result = func.Invoke(useTempTable);
            }
            return result;
        }

        /// <summary>
        /// Bulk update entity
        /// </summary>
        /// <typeparam name="TInsert"></typeparam>
        /// <param name="updatedEntities"></param>
        /// <param name="tableSchemasAndName"></param>
        public static void BulkInsertUpdateDeleteEntities<TInsert, TUpdate>(string connection, List<TInsert> insertedEntities, List<TUpdate> updatedEntities, List<Guid> deletedEntities, string tableSchemasAndName)
        {
            if (insertedEntities.IsNullOrEmpty() && updatedEntities.IsNullOrEmpty() && deletedEntities.IsNullOrEmpty()) return;
            using (var sqlConn = new SqlConnection(connection))
            {
                sqlConn.Open();
                using (SqlTransaction trans = sqlConn.BeginTransaction())
                {
                    using (SqlCommand cmd = new SqlCommand(string.Empty, sqlConn, trans))
                    {
                        try
                        {
                            if (updatedEntities != null && updatedEntities.Any())
                            {
                                var columns = GetEntityColumns<TUpdate>();
                                var tempTableId = Guid.NewGuid().ToString().Replace("-", "");
                                // create temp table
                                string tableTempUpdate = $"#{tempTableId}";
                                cmd.CommandText = $"Select top 1 {string.Join(", ", columns)} into {tableTempUpdate} from {tableSchemasAndName} where Id is null";
                                cmd.ExecuteNonQuery();

                                BulkCopyDataToTable(updatedEntities, tableTempUpdate, columns, sqlConn, trans);

                                cmd.CommandText = UpdateEntityQuery(tableTempUpdate, tableSchemasAndName, columns);
                                cmd.ExecuteNonQuery();
                            }

                            if (deletedEntities != null && deletedEntities.Any())
                            {
                                // create temp table
                                var lookupModels = new List<Common.TempTableData>(deletedEntities.Distinct().Select(x => new Common.TempTableData { Id = x }).ToList());
                                var tempTableId = Guid.NewGuid().ToString().Replace("-", "");
                                string tableTempDelete = $"#{tempTableId}";
                                cmd.CommandText = $"CREATE TABLE {tableTempDelete}(Id uniqueidentifier)";
                                cmd.ExecuteNonQuery();

                                BulkCopyDataToTable(lookupModels, tableTempDelete, new List<string> { nameof(Common.TempTableData.Id) }, sqlConn, trans);

                                cmd.CommandText = DeleteEntityQuery(tableTempDelete, tableSchemasAndName);
                                cmd.ExecuteNonQuery();
                            }

                            // insert 
                            if (insertedEntities != null && insertedEntities.Any())
                            {
                                BulkInsertDataToTable(insertedEntities, tableSchemasAndName, sqlConn, trans, cmd);
                            }

                            trans.Commit();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            throw;
                        }
                        finally
                        {
                            sqlConn.Close();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Bulk insert update entity async
        /// </summary>
        /// <typeparam name="TInsert"></typeparam>
        /// <typeparam name="TUpdate"></typeparam>
        /// <param name="connection"></param>
        /// <param name="insertedEntities"></param>
        /// <param name="updatedEntities"></param>
        /// <param name="deletedEntities"></param>
        /// <param name="tableSchemasAndName"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static async Task<int> BulkInsertUpdateDeleteEntitiesAsync<TInsert, TUpdate>(string connection, List<TInsert> insertedEntities, List<TUpdate> updatedEntities, List<Guid> deletedEntities, string tableSchemasAndName, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            int rowEffects = 0;

            if (insertedEntities.IsNullOrEmpty() && updatedEntities.IsNullOrEmpty() && deletedEntities.IsNullOrEmpty()) return rowEffects;
            using (var sqlConn = new SqlConnection(connection))
            {
                await sqlConn.OpenAsync();
                using (SqlTransaction trans = (SqlTransaction)await sqlConn.BeginTransactionAsync())
                {
                    using (SqlCommand cmd = new SqlCommand(string.Empty, sqlConn, trans))
                    {
                        try {
                            if (updatedEntities != null && updatedEntities.Any())
                            {
                                rowEffects = updatedEntities.Count();
                                var columns = GetEntityColumns<TUpdate>();
                                var tempTableId = Guid.NewGuid().ToString().Replace(CommonConstants.HyphenSymbol, CommonConstants.ReplaceEmpty);
                                // create temp table
                                string tableTempUpdate = $"#{tempTableId}";
                                cmd.CommandText = $"Select top 1 {string.Join(CommonConstants.CommasAndSpace, columns)} into {tableTempUpdate} from {tableSchemasAndName} where Id is null";
                                await cmd.ExecuteNonQueryAsync();

                                await BulkCopyDataToTableAsync(updatedEntities, tableTempUpdate, columns, sqlConn, trans, batchSize);

                                cmd.CommandText = UpdateEntityQuery(tableTempUpdate, tableSchemasAndName, columns);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            if (deletedEntities != null && deletedEntities.Any())
                            {
                                rowEffects = deletedEntities.Count();
                                // create temp table
                                var lookupModels = new List<Common.TempTableData>(deletedEntities.Distinct().Select(x => new Common.TempTableData { Id = x }).ToList());
                                var tempTableId = Guid.NewGuid().ToString().Replace(CommonConstants.HyphenSymbol, CommonConstants.ReplaceEmpty);
                                string tableTempDelete = $"#{tempTableId}";
                                cmd.CommandText = $"CREATE TABLE {tableTempDelete}(Id uniqueidentifier)";
                                await cmd.ExecuteNonQueryAsync();

                                await BulkCopyDataToTableAsync(lookupModels, tableTempDelete, new List<string> { nameof(Common.TempTableData.Id) }, sqlConn, trans, batchSize);

                                cmd.CommandText = DeleteEntityQuery(tableTempDelete, tableSchemasAndName);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // insert
                            if (insertedEntities != null && insertedEntities.Any())
                            {
                                rowEffects = insertedEntities.Count();
                                await BulkInsertDataToTableAsync(insertedEntities, tableSchemasAndName, sqlConn, trans, cmd, batchSize);
                            }

                            await trans.CommitAsync();
                            return rowEffects;
                        }
                        catch (Exception ex) {
                            await trans.RollbackAsync();
                            throw;
                        }
                        finally {
                            await sqlConn.CloseAsync();
                        }
                    }
                }
            }
        }

        /// Get EntityColumns for Bulk insert
        /// </summary>
        /// <typeparam name="TEntityModel"></typeparam>
        public static List<string> GetEntityColumns<TEntityModel>()
        {
            List<string> columns = new List<string>();
            foreach (var property in typeof(TEntityModel).GetProperties()) {
                if (property.CustomAttributes != null && property.CustomAttributes.Any(x => x.AttributeType == typeof(NotMappedAttribute) || x.AttributeType == typeof(ReadOnlyAttribute)))
                    continue;
                if (property.PropertyType.Namespace != nameof(System) && !property.PropertyType.IsEnum) continue;
                columns.Add(property.Name);
            }
            return columns.Distinct().ToList();
        }

        /// <summary>
        /// Check if table is exist
        /// </summary>
        /// <param name="tableNameAndSchema"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async static Task<bool> IsDbTableExistAsync(string tableNameAndSchema, string connection)
        {
            using (SqlConnection sqlConn = new SqlConnection(connection)) {
                await sqlConn.OpenAsync();
                using (SqlTransaction trans = sqlConn.BeginTransaction()) {
                    string checkTable = String.Format(
                      "IF OBJECT_ID('{0}', 'U') IS NOT NULL SELECT 'true' ELSE SELECT 'false'",
                      tableNameAndSchema);
                    using (SqlCommand cmd = new SqlCommand(checkTable, sqlConn, trans)) {
                        try {
                            cmd.CommandType = CommandType.Text;
                            var result = await cmd.ExecuteScalarAsync();
                            return Convert.ToBoolean(result);
                        }
                        catch (Exception ex) {
                            trans.Rollback();
                            throw;
                        }
                        finally {
                            await sqlConn.CloseAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Bulk copy data to table
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableTempUpdate"></param>
        /// <param name="columns"></param>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        private static void BulkCopyDataToTable<TEntity>(List<TEntity> entities, string tableTempUpdate, List<string> columns, SqlConnection sqlConn, SqlTransaction trans, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            entities = entities.DistinctBy(x => (Guid)x.GetValueIgnoreCase("Id")).ToList();
            var dataTable = new ListDataReader<TEntity>(entities, columns.ToArray());

            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, trans))
            {
                bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                bulkcopy.BatchSize = batchSize;
                bulkcopy.DestinationTableName = tableTempUpdate;
                foreach (var col in columns)
                {
                    bulkcopy.ColumnMappings.Add(col, col);
                }
                bulkcopy.WriteToServer(dataTable);
                bulkcopy.Close();
            }
        }

        /// <summary>
        /// BulkCopyDataToTableAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableTempUpdate"></param>
        /// <param name="columns"></param>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        private static async Task BulkCopyDataToTableAsync<TEntity>(List<TEntity> entities, string tableTempUpdate, List<string> columns, SqlConnection sqlConn, SqlTransaction trans, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            entities = entities.DistinctBy(x => (Guid)x.GetValueIgnoreCase("Id")).ToList();
            var dataTable = new ListDataReader<TEntity>(entities, columns.ToArray());

            using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, trans))
            {
                bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                bulkcopy.BatchSize = batchSize;
                bulkcopy.DestinationTableName = tableTempUpdate;
                foreach (var col in columns)
                {
                    bulkcopy.ColumnMappings.Add(col, col);
                }
                await bulkcopy.WriteToServerAsync(dataTable);
                bulkcopy.Close();
            }
        }


        /// <summary>
        /// Bulk Insert Data To Table. For large data, we will use temp table to insert data first and then copy data to main table
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableInsert"></param>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="batchSize"></param>
        private static void BulkInsertDataToTable<TEntity>(List<TEntity> entities, string tableInsert, SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            try {
                var columns = GetEntityColumns<TEntity>();
                entities = entities.DistinctBy(x => (Guid)x.GetValueIgnoreCase("Id")).ToList();
                var dataTable = new ListDataReader<TEntity>(entities, columns.ToArray());
                var desTableColumns = GetDbTableColumns(tableInsert, sqlCommand);
                if (!columns.Any()) return;
                if (entities.Count > CommonConstants.MinimumRowForApplyTempTableInsert)
                {
                    var tempTableId = Guid.NewGuid().ToString().Replace(oldValue: "-", "");
                    string tableTempInsert = $"#{typeof(TEntity).Name}_{tempTableId}";
                    sqlCommand.CommandText = $@"SELECT TOP 0 {string.Join(", ", desTableColumns.Select(columnName => $"[{columnName}]"))} INTO {tableTempInsert} FROM {tableInsert}
                                                  ALTER TABLE {tableTempInsert} ADD RowNumber INT Identity (1,1);
                                                  CREATE CLUSTERED INDEX [INDEX_{tableTempInsert}] ON {tableTempInsert} (RowNumber ASC);";
                    sqlCommand.ExecuteNonQuery();
                    // insert data to temp table
                    using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, trans))
                    {
                        bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                        bulkcopy.BatchSize = batchSize;
                        bulkcopy.DestinationTableName = tableTempInsert;
                        foreach (var descCol in desTableColumns)
                        {
                            var sourceColumnName = columns.FirstOrDefault(x => string.Equals(x, descCol, StringComparison.InvariantCultureIgnoreCase));
                            if (!string.IsNullOrWhiteSpace(sourceColumnName))
                            {
                                bulkcopy.ColumnMappings.Add(sourceColumnName, descCol);
                            }
                        }
                        //Save to temptable first
                        bulkcopy.WriteToServer(dataTable);
                        bulkcopy.Close();
                    }
                    // copy data from temp table to main table
                    var pageCount = (double)entities.Count / batchSize;
                    int ceilingPage = (int)Math.Ceiling(pageCount);

                    for (int currentPage = 0; currentPage < ceilingPage; currentPage++)
                    {
                        var begin = currentPage * batchSize;
                        var end = begin + batchSize;
                        sqlCommand.CommandText = InsertEntityQuery(tableTempInsert, tableInsert, desTableColumns, begin, end);
                        if (currentPage == ceilingPage)
                        {
                            sqlCommand.CommandText = $@"{sqlCommand.CommandText}
                                                         DROP TABLE {tableTempInsert}"; // drop table if there is no page left
                        }
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                else
                {
                    // direct copy to main table if we have less records than 10 rows
                    using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, trans))
                    {
                        bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                        bulkcopy.BatchSize = entities.Count;
                        bulkcopy.DestinationTableName = tableInsert;
                        foreach (var descCol in desTableColumns)
                        {
                            var sourceColumnName = columns.FirstOrDefault(x => string.Equals(x, descCol, StringComparison.InvariantCultureIgnoreCase));
                            if (!string.IsNullOrWhiteSpace(sourceColumnName))
                            {
                                bulkcopy.ColumnMappings.Add(sourceColumnName, descCol);
                            }
                        }
                        bulkcopy.WriteToServer(dataTable);
                        bulkcopy.Close();
                    }
                }
            }
            catch (Exception ex) {
                throw;
            }
        }

        /// <summary>
        /// BulkInsertDataToTableAsync
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="tableInsert"></param>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        private static async Task BulkInsertDataToTableAsync<TEntity>(List<TEntity> entities, string tableInsert, SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            try {
                var columns = GetEntityColumns<TEntity>();
                entities = entities.DistinctBy(x => (Guid)x.GetValueIgnoreCase("Id")).ToList();
                var dataTable = new ListDataReader<TEntity>(entities, columns.ToArray());
                var desTableColumns = GetDbTableColumns(tableInsert, sqlCommand);
                if (!columns.Any()) return;

                if (entities.Count > CommonConstants.MinimumRowForApplyTempTableInsert) {
                    var tempTableId = Guid.NewGuid().ToString().Replace(oldValue: CommonConstants.HyphenSymbol, CommonConstants.ReplaceEmpty);
                    string tableTempInsert = $"#{typeof(TEntity).Name}_{tempTableId}";
                    sqlCommand.CommandText = $@"SELECT TOP 0 {string.Join(CommonConstants.CommasAndSpace, desTableColumns.Select(columnName => $"[{columnName}]"))} INTO {tableTempInsert} FROM {tableInsert}
                                                  ALTER TABLE {tableTempInsert} ADD RowNumber INT Identity (1,1);
                                                  CREATE CLUSTERED INDEX [INDEX_{tableTempInsert}] ON {tableTempInsert} (RowNumber ASC);";
                    await sqlCommand.ExecuteNonQueryAsync();
                    // insert data to temp table
                    using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, trans)) {
                        bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                        bulkcopy.BatchSize = batchSize;
                        bulkcopy.DestinationTableName = tableTempInsert;
                        foreach (var descCol in desTableColumns) {
                            var sourceColumnName = columns.FirstOrDefault(x => string.Equals(x, descCol, StringComparison.InvariantCultureIgnoreCase));
                            if (!string.IsNullOrWhiteSpace(sourceColumnName)) {
                                bulkcopy.ColumnMappings.Add(sourceColumnName, descCol);
                            }
                        }
                        await bulkcopy.WriteToServerAsync(dataTable);
                        bulkcopy.Close();
                    }
                    // copy data from temp table to main table
                    var pageCount = (double)entities.Count / batchSize;
                    int ceilingPage = (int)Math.Ceiling(pageCount);

                    for (int currentPage = 0; currentPage < ceilingPage; currentPage++) {
                        var begin = currentPage * batchSize;
                        var end = begin + batchSize;
                        sqlCommand.CommandText = InsertEntityQuery(tableTempInsert, tableInsert, desTableColumns, begin, end);
                        if (currentPage == ceilingPage) {
                            sqlCommand.CommandText = $@"{sqlCommand.CommandText}
                                                         DROP TABLE {tableTempInsert}"; // drop table if there is no page left
                        }
                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                }
                else {
                    // direct copy to main table if we have less records than 10 rows
                    using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, trans)) {
                        bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                        bulkcopy.BatchSize = entities.Count;
                        bulkcopy.DestinationTableName = tableInsert;
                        foreach (var descCol in desTableColumns) {
                            var sourceColumnName = columns.FirstOrDefault(x => string.Equals(x, descCol, StringComparison.InvariantCultureIgnoreCase));
                            if (!string.IsNullOrWhiteSpace(sourceColumnName)) {
                                bulkcopy.ColumnMappings.Add(sourceColumnName, descCol);
                            }
                        }
                        await bulkcopy.WriteToServerAsync(dataTable);
                        bulkcopy.Close();
                    }
                }
            }
            catch (Exception ex) {
                throw;
            }
        }

        /// <summary>
        /// BulkInsert DataTable To Table
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dataTable"></param>
        /// <param name="tableInsert"></param>
        /// <param name="sqlConn"></param>
        /// <param name="listColumnMappings"></param>
        /// <param name="batchSize"></param>
        private static void BulkInsertDataTableToTable(DataTable dataTable, string tableInsert, SqlConnection sqlConn, List<SqlBulkCopyColumnMapping> listColumnMappings, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            try {
                // insert data to temp table
                using (SqlBulkCopy bulkcopy = new SqlBulkCopy(sqlConn)) {
                    bulkcopy.BulkCopyTimeout = CommonConstants.DbBulkUpdateTimeout;
                    bulkcopy.BatchSize = batchSize;
                    bulkcopy.DestinationTableName = tableInsert;
                    foreach (var mapping in listColumnMappings) {
                        bulkcopy.ColumnMappings.Add(mapping);
                    }
                    bulkcopy.WriteToServer(dataTable);
                    bulkcopy.Close();
                }
            }
            catch (Exception ex) {
                throw;
            }
        }

        /// <summary>
        /// Get real database column schemas
        /// </summary>
        /// <param name="tableInsert"></param>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        private static List<string> GetDbTableColumns(string tableInsert, SqlCommand sqlCommand)
        {
            var columns = new List<string>();
            // create temp table
            sqlCommand.CommandText = $"Select top 0 * from {tableInsert}";
            using (var reader = sqlCommand.ExecuteReader()) {
                var tableSchemas = reader.GetSchemaTable();

                foreach (DataRow row in tableSchemas.Rows) {
                    string? columnName = row.Field<string>("ColumnName");

                    if (!string.IsNullOrEmpty(columnName)) {
                        columns.Add(columnName);
                    }
                }
            }
            return columns;
        }

        /// <summary>
        /// Insert data get from temptable
        /// </summary>
        /// <param name="tableTempName"></param>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static string InsertEntityQuery(string tableTempName, string tableName, List<string> columns, int begin, int end)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var listFieldInserted = string.Join(", ", columns.Select(columnName => $"[{columnName}]"));

            stringBuilder.AppendLine($@"INSERT INTO {tableName} ({listFieldInserted})
	                                    SELECT {listFieldInserted} FROM {tableTempName} WHERE RowNumber > {begin} AND RowNumber <= {end}");

            return stringBuilder.ToString();
        }

        /// </summary>
        /// <param name="tableTempName"></param>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static string UpdateEntityQuery(string tableTempName, string tableName, List<string> columns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"UPDATE {tableName} SET ");
            var listUpdated = columns.Where(x => !string.Equals(x, "Id", StringComparison.InvariantCultureIgnoreCase))
                                     .Select(columnName => $"{columnName} = source.{columnName}");
            stringBuilder.AppendLine(string.Join(", ", listUpdated));
            stringBuilder.AppendLine($"FROM {tableName} as target ");
            stringBuilder.AppendLine($"INNER JOIN {tableTempName} as source on source.Id = target.Id ");
            stringBuilder.AppendLine($"DROP TABLE {tableTempName}");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Build update entity query command
        /// </summary>
        /// <param name="tableTempName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static string DeleteEntityQuery(string tableTempName, string tableName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"DELETE target FROM {tableName} as target");
            stringBuilder.AppendLine($"INNER JOIN {tableTempName} as source on source.Id = target.Id ");
            stringBuilder.AppendLine($"DROP TABLE {tableTempName}");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Get entity from dbcontext
        /// </summary>
        /// <param name="dbDbContext"></param>
        /// <param name="entityState"></param>
        /// <returns></returns>
        public static Dictionary<Type, List<object>> GetDictionaryEntityFromDbContext(this Microsoft.EntityFrameworkCore.DbContext dbContext, params EntityState[] entityState)
        {
            return dbContext.ChangeTracker.Entries()
                                            .Where(x => entityState.Contains(x.State))
                                            .GroupBy(x => x.Entity.GetType())
                                            .ToDictionary(x => x.Key, x => x.Select(y => y.Entity).ToList());
        }

        /// <summary>
        /// Bulk Insert With Transaction
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="insertedEntities"></param>
        /// <param name="schemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static int BulkAddUpdateDeleteEntity<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, (List<TEntity>, List<TEntity>, List<TEntity>) data, SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, BulkInsertConfig bulkInsertConfig)
        {
            int rowEffects = 0;
            if (bulkInsertConfig == null)
                return rowEffects;
            var (insertList, updateList, deleteList) = data;
            var tableName = bulkInsertConfig.CustomTableName ?? dbContext.GetTableName<TEntity>();
            if (insertList.Any()) {
                rowEffects += sqlConn.AddBulkInsert(trans, sqlCommand, insertList, bulkInsertConfig.TableSchemas, bulkInsertConfig.BatchSize, tableName);
            }
            if (updateList.Any()) {
                rowEffects += sqlConn.AddBulkUpdate(trans, sqlCommand, updateList, bulkInsertConfig.TableSchemas, columns: bulkInsertConfig.ColumnUpdates, batchSize: bulkInsertConfig.BatchSize, tableName: tableName);
            }
            if (deleteList.Any()) {
                var deleteGuids = deleteList.Select(x => (Guid)x.GetValueIgnoreCase("Id")).ToList();
                rowEffects += sqlConn.AddBulkDelete<TEntity>(trans, sqlCommand, deleteGuids, bulkInsertConfig.TableSchemas, bulkInsertConfig.BatchSize, tableName);
            }
            if (bulkInsertConfig.ListEntityNeedDetached != null) {
                bulkInsertConfig.ListEntityNeedDetached.Add(typeof(TEntity));
            }
            return rowEffects;
        }

        /// <summary>
        /// Bulk Insert With Transaction
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="insertedEntities"></param>
        /// <param name="schemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static int BulkAddUpdateEntity<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, (List<TEntity>, List<TEntity>) data, SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, BulkInsertConfig bulkInsertConfig)
        {
            int rowEffects = 0;
            if (bulkInsertConfig == null)
                return rowEffects;
            var (insertList, updateList) = data;
            var tableName = bulkInsertConfig.CustomTableName ?? dbContext.GetTableName<TEntity>();
            if (insertList.Any()) {
                rowEffects += sqlConn.AddBulkInsert(trans, sqlCommand, insertList, bulkInsertConfig.TableSchemas, bulkInsertConfig.BatchSize, tableName);
            }
            if (updateList.Any()) {
                rowEffects += sqlConn.AddBulkUpdate(trans, sqlCommand, updateList, bulkInsertConfig.TableSchemas, columns: bulkInsertConfig.ColumnUpdates, batchSize: bulkInsertConfig.BatchSize, tableName: tableName);
            }
            if (bulkInsertConfig.ListEntityNeedDetached != null) {
                bulkInsertConfig.ListEntityNeedDetached.Add(typeof(TEntity));
            }
            return rowEffects;
        }

        /// <summary>
        /// BulkSaveChangeWithoutTransactions
        /// </summary>
        /// <param name="dbDbContext"></param>
        /// <param name="action"></param>
        /// <param name="detachedAfterComplete"></param>
        /// <returns></returns>
        public static int BulkSaveChangeWithoutTransaction(this Microsoft.EntityFrameworkCore.DbContext dbDbContext, Func<SqlConnection, SqlCommand, int> action, SqlConnection sqlConn)
        {
            return RetryExtension.DoRetryReturnResult(() =>
            {
                int rowEffects = 0;
                if (sqlConn.State != ConnectionState.Open) {
                    sqlConn.Open();
                }
                using (SqlCommand cmd = new SqlCommand(string.Empty, sqlConn)) {
                    cmd.CommandTimeout = CommonConstants.DbBulkUpdateTimeout;
                    try {
                        rowEffects = action.Invoke(sqlConn, cmd);
                    }
                    catch (Exception ex) {
                        throw;
                    }
                }
                return rowEffects;
            }, (ex) => IsDeadLockException(ex), TimeSpan.FromSeconds(10), 3);
        }


        /// <summary>
        /// check exception is timeout
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static bool IsTimeoutException(Exception ex)
        {
            var sqlException = ex as SqlException;
            return (sqlException != null && (sqlException.Number == -2 || sqlException.Message.Contains("Execution Timeout Expired"))) || ex.Message.Contains("Execution Timeout Expired");
        }


        /// <summary>
        /// Bulk Insert With Transaction
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="insertedEntities"></param>
        /// <param name="schemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static int AddBulkInsert<TEntity>(this SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, List<TEntity> insertedEntities, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize, string? tableName = null)
        {
            if (insertedEntities != null && insertedEntities.Any()) {
                tableName ??= GetTableName<TEntity>();
                var tableSchemasAndName = $"[{schemas}].[{tableName}]";
                BulkInsertDataToTable(insertedEntities, tableSchemasAndName, sqlConn, trans, sqlCommand, batchSize);
                return insertedEntities.Count();
            }
            return 0;
        }

        public static int AddBulkInsertDataTable(this SqlConnection sqlConn, DataTable dataTable, string tableName, List<SqlBulkCopyColumnMapping> listColumnMappings, int batchSize = CommonConstants.SqlBulkCopyBatchSize)
        {
            if (dataTable != null && dataTable.Rows.Count > 0) {
                BulkInsertDataTableToTable(dataTable, tableName, sqlConn, listColumnMappings, batchSize);
                return dataTable.Rows.Count;
            }
            return 0;
        }

        /// <summary>
        /// Get real table name
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static string? GetTableName<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            return dbContext.Model.FindEntityType(typeof(TEntity))?.GetTableName();
        }

        private static string GetTableName<TEntity>()
        {
            var tableName = typeof(TEntity).Name;
            var tableAttribute = typeof(TEntity)
                .GetCustomAttributes(typeof(TableAttribute), false)
                .Cast<TableAttribute>()
                .FirstOrDefault();
            if (tableAttribute != null) {
                tableName = tableAttribute.Name;
            }
            return tableName;
        }

        /// <summary>
        /// Bulk Insert With Transaction
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="insertedEntities"></param>
        /// <param name="schemas"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static int AddBulkInsertWithOption<TEntity>(this Microsoft.EntityFrameworkCore.DbContext dbContext, SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, List<TEntity> insertedEntities, BulkInsertConfig bulkInsertConfig) where TEntity : class
        {
            if (bulkInsertConfig == null || !bulkInsertConfig.IsValid() || dbContext == null)
            {
                throw new ArgumentException($"Unexpected empty argument.");
            }
            var tableName = bulkInsertConfig.CustomTableName ?? dbContext.GetTableName<TEntity>();
            int rowEffects = sqlConn.AddBulkInsert(trans, sqlCommand, insertedEntities, bulkInsertConfig.TableSchemas, bulkInsertConfig.BatchSize, tableName);

            return rowEffects;
        }

        /// <summary>
        /// EnableAutoDetectChanges
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="enable"></param> 
        public static void EnableAutoDetectChanges(this Microsoft.EntityFrameworkCore.DbContext dbContext, bool enable)
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = enable;
        }

        /// <summary>
        /// Bulk Update With Transaction
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sqlConn"></param>
        /// <param name="trans"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="updateEntities"></param>
        /// <param name="tableSchemasAndName"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static int AddBulkUpdate<TEntity>(this SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, List<TEntity> updateEntities, string schemas, List<string>? columns = null, int batchSize = CommonConstants.SqlBulkCopyBatchSize, string? tableName = null)
        {
            if (updateEntities != null && updateEntities.Any()) {
                tableName ??= GetTableName<TEntity>();
                var tableSchemasAndName = $"[{schemas}].[{tableName}]";
                columns ??= GetEntityColumns<TEntity>();
                // create temp table
                var tempTableId = Guid.NewGuid().ToString().Replace("-", "");
                string tableTempUpdate = $"#{tempTableId}";
                sqlCommand.CommandText = $"Select top 0 {string.Join(", ", columns)} into {tableTempUpdate} from {tableSchemasAndName}";
                sqlCommand.ExecuteNonQuery();

                BulkCopyDataToTable(updateEntities, tableTempUpdate, columns, sqlConn, trans, batchSize);

                sqlCommand.CommandText = UpdateEntityQuery(tableTempUpdate, tableSchemasAndName, columns);
                return sqlCommand.ExecuteNonQuery();
            }
            return 0;
        }

        public static int AddBulkDelete<TEntity>(this SqlConnection sqlConn, SqlTransaction trans, SqlCommand sqlCommand, List<Guid> deleteEntities, string schemas, int batchSize = CommonConstants.SqlBulkCopyBatchSize, string? tableName = null)
        {
            if (deleteEntities != null && deleteEntities.Any()) {
                tableName ??= GetTableName<TEntity>();
                var tableSchemasAndName = $"[{schemas}].[{tableName}]";
                var lookupModels = new List<Common.TempTableData>(deleteEntities.Distinct().Select(x => new Common.TempTableData { Id = x }).ToList());
                var tempTableId = Guid.NewGuid().ToString().Replace("-", "");
                string tableTempDelete = $"#{tempTableId}";
                sqlCommand.CommandText = $"CREATE TABLE {tableTempDelete}(Id uniqueidentifier , PRIMARY KEY (id))";
                sqlCommand.ExecuteNonQuery();

                BulkCopyDataToTable(lookupModels, tableTempDelete, new List<string> { nameof(Common.TempTableData.Id) }, sqlConn, trans, batchSize);

                sqlCommand.CommandText = DeleteEntityQuery(tableTempDelete, tableSchemasAndName);
                return sqlCommand.ExecuteNonQuery();
            }
            return 0;
        }

        /// <summary>
        /// Detach entities
        /// </summary>
        /// <param name="entityStates"></param>
        public static void DetachAllEntitiesByType(this Microsoft.EntityFrameworkCore.DbContext dbContext, params Type[] types)
        {
            foreach (var type in types) {
                var changedEntriesCopy = dbContext.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Unchanged && e.Entity.GetType() == type)
                .ToList();

                foreach (var entry in changedEntriesCopy)
                    entry.State = EntityState.Detached;
            }
        }

        /// <summary>
        /// Apply shard number for entities
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="dbContext"></param>
        /// <param name="shardNumber"></param>
        /// <returns></returns>
        public static IQueryable<TEntity> WithShardNumber<TEntity>(this IQueryable<TEntity> query, Microsoft.EntityFrameworkCore.DbContext dbContext, int shardNumber) where TEntity : class
        {
            if (shardNumber == 0) {
                return query;
            }

            var queryString = query.ToQueryString();
            queryString = queryString.Replace($"[{typeof(TEntity).Name}]", $"[{typeof(TEntity).Name}{shardNumber}]");
            var newQuery = dbContext.Set<TEntity>().FromSqlRaw(queryString);

            return newQuery;
        }
    }
}
