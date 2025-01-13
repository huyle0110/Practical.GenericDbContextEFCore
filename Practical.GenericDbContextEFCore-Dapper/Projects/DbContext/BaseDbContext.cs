using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Practical.GenericDbContextEFCore.Common;
using Practical.GenericDbContextEFCore.Extension;
using Practical.GenericDbContextEFCore.Service.Interface;
using System.Linq.Expressions;
using System.Reflection;

namespace Practical.GenericDbContextEFCore.DbContext
{
    public class BaseDbContext : Microsoft.EntityFrameworkCore.DbContext, IChangedDataContext
    {
        private readonly ICacheData _cacheData;
        private static readonly string[] defaultFieldsUpdated = new string[]
        {
            nameof(DefaultEntityFieldsUpdatedEnum.LastSavedTime),
            nameof(DefaultEntityFieldsUpdatedEnum.LastSavedUser),
        };

        public IQueryable<Guid> TempTableData => Set<TempTableData>().Select(t => t.Id);
        public IQueryable<string> TempTableDataString => Set<TempTableDataString>().Select(t => t.Id);
        public IQueryable<int> TempTableDataIntValue => Set<TempTableDataIntValue>().Select(t => t.Id);

        public IQueryable<TempTableDataForGuidIdAndIntValue> TempTableDataForGuidIdAndIntValue => Set<TempTableDataForGuidIdAndIntValue>().Select(t => t);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TempTableData>()
                  .ToView($"#{nameof(TempTableData)}");

            modelBuilder.Entity<TempTableDataString>()
                  .ToView($"#{nameof(TempTableDataString)}");

            modelBuilder.Entity<TempTableDataForGuidIdAndIntValue>()
                  .ToView($"#{nameof(TempTableDataForGuidIdAndIntValue)}");

            modelBuilder.Entity<TempTableDataIntValue>()
                  .ToView($"#{nameof(TempTableDataIntValue)}");
        }

        /// <summary>
        /// Get dbcontext schemas
        /// </summary>
        /// <returns></returns>
        public virtual string GetSchemas()
        {
            return null;
        }

        public BaseDbContext(DbContextOptions options) : base(options)
        {
            if (Database != null)
            {
                Database.SetCommandTimeout(CommonConstants.CommandTimeout);
            }
        }

        public BaseDbContext(DbContextOptions options, ICacheData cacheData) : base(options)
        {
            if (Database != null)
            {
                Database.SetCommandTimeout(CommonConstants.CommandTimeout);
            }
            _cacheData = cacheData ?? throw new ArgumentNullException(nameof(cacheData));
        }

        public BaseDbContext()
        {
            if (Database != null)
            {
                Database.SetCommandTimeout(CommonConstants.CommandTimeout);
            }
        }

        public virtual DbSet<T> Repository<T>() where T : class
        {
            return Set<T>();
        }

        public void UpdateProperty<T>() where T : class
        {
            var createdSourceInfo =
                ChangeTracker.Entries<T>().Where(e => e.State == EntityState.Added);
            foreach (var entry in createdSourceInfo) {
                entry.Property("CreatedTime").CurrentValue = DateTime.UtcNow;
                entry.Property("LastSavedTime").CurrentValue = DateTime.UtcNow;
            }
            var modifiedSourceInfo =
                ChangeTracker.Entries<T>().Where(e => e.State == EntityState.Modified);
            if (modifiedSourceInfo != null) {
                foreach (var entry in modifiedSourceInfo) {
                    entry.Property("LastSavedTime").CurrentValue = DateTime.UtcNow;
                }
            }
        }

        public void CustomizeInitContextBuider(ModelBuilder builder, string defaultSchema, bool useLowerCase = true)
        {
            builder.HasDefaultSchema(defaultSchema);
            base.OnModelCreating(builder);
            if (useLowerCase) {
                LowerCaseForAllEntityInSchema(builder);
            }
        }

        public void CustomizeInitContextBuider(ModelBuilder builder, bool useLowerCase = true)
        {
            base.OnModelCreating(builder);
            if (useLowerCase) {
                LowerCaseForAllEntityInSchema(builder);
            }
        }

        public void LowerCaseForAllEntityInSchema(ModelBuilder builder)
        {
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                if (entity.IsKeyless)
                {
                }
                else
                {
                    var currentTableName = builder.Entity(entity.Name).Metadata.GetTableName();
                    builder.Entity(entity.Name).ToTable(currentTableName);
                }
            }
        }

        public int SaveChangesWithoutUpdateProperty()
        {
            return base.SaveChanges();
        }

        public override int SaveChanges()
        {
            var isEmptyCache = _cacheData == null || _cacheData.IsEmpty();

            var changedEntries = isEmptyCache ? null : base.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .GroupBy(e => e.Metadata.Name)
                .Select(g => g.First())
                .Select(e => e.Entity.GetType());

            var result = base.SaveChanges();

            if (!isEmptyCache) {
                _cacheData?.Clear(changedEntries);
            }

            return result;
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            var isEmptyCache = _cacheData == null || _cacheData.IsEmpty();

            var changedEntries = isEmptyCache ? null : base.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .GroupBy(e => e.Metadata.Name)
                .Select(g => g.First())
                .Select(e => e.Entity.GetType());

            var result = await base.SaveChangesAsync();

            if (!isEmptyCache) {
                _cacheData?.Clear(changedEntries);
            }

            return result;
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            return await base.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        /// <summary>
        /// Update changed fields only for an entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public virtual void UpdateEntity<T>(T entity) where T : class
        {
            var entityType = entity.GetType();
            IList<string> changedFields = new List<string>();

            if (entityType.GetProperties().Any(x => x.Name == nameof(EntityObjectState.ChangedFields))) {
                changedFields = (entityType.GetProperty(nameof(EntityObjectState.ChangedFields))?.GetValue(entity) as List<string>) ?? new List<string>();
            }

            this.HandleEntityUpdateChangedFields(entity, changedFields, defaultFieldsUpdated);
        }

        /// <summary>
        /// Update changed fields only for entities
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        public virtual void UpdateRangeEntity<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities) {
                UpdateEntity(entity);
            }
        }

        /// <summary>
        /// Update changed fields only for an entity by property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="properties"></param>
        public virtual void UpdateEntity<T>(T entity, params Expression<Func<T, object>>[] properties) where T : class
        {
            var changedFields = new List<string>();
            if (properties.Any())
            {
                foreach (var prop in properties)
                {
                    var propName = prop.GetPropertyName();
                    if (!string.IsNullOrEmpty(propName))
                        changedFields.Add(propName);
                }
            }

            this.HandleEntityUpdateChangedFields(entity, changedFields, defaultFieldsUpdated);
        }
        /// <summary>
        /// Change State
        /// </summary>
        /// <returns></returns>
        public void SyncObjectsStatePostCommit()
        {
            foreach (var dbEntityEntry in ChangeTracker.Entries())
            {
                var entityObject = dbEntityEntry.Entity as IObjectState;
                if (entityObject != null)
                {
                    entityObject.ObjectState = StateHelper.ConvertState(dbEntityEntry.State);
                }
            }
        }

        /// <summary>
        /// use pure SaveChanges of EF
        /// </summary>
        /// <returns></returns>
        public int SaveChangesEF()
        {
            return base.SaveChanges();
        }

        /// <summary>
        /// use pure SaveChangesAsync of EF
        /// </summary>
        /// <returns></returns>
        public async Task<int> SaveChangesEFAsync()
        {
            return await base.SaveChangesAsync();
        }

        /// <summary>
        /// use pure SaveChangesAsync of EF
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> SaveChangesEFAsync(CancellationToken cancellationToken)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        public void SyncObjectState<TEntity>(TEntity entity) where TEntity : class, IObjectState
        {
            Entry(entity).State = StateHelper.ConvertState(entity.ObjectState);
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        /// </summary>
        /// <exception cref="System.Data.Entity.Infrastructure.DbUpdateException">
        ///     An error occurred sending updates to the database.</exception>
        /// <exception cref="System.Data.Entity.Infrastructure.DbUpdateConcurrencyException">
        ///     A database command did not affect the expected number of rows. This usually
        ///     indicates an optimistic concurrency violation; that is, a row has been changed
        ///     in the database since it was queried.</exception>
        /// <exception cref="System.Data.Entity.Validation.DbEntityValidationException">
        ///     The save was aborted because validation of entity property values failed.</exception>
        /// <exception cref="System.NotSupportedException">
        ///     An attempt was made to use unsupported behavior such as executing multiple
        ///     asynchronous commands concurrently on the same context instance.</exception>
        /// <exception cref="System.ObjectDisposedException">
        ///     The context or connection have been disposed.</exception>
        /// <exception cref="System.InvalidOperationException">
        ///     Some error occurred attempting to process entities in the context either
        ///     before or after sending commands to the database.</exception>
        /// <seealso cref="DbContext.SaveChangesAsync"/>
        /// <returns>A task that represents the asynchronous save operation.  The 
        ///     <see cref="Task.Result">Task.Result</see> contains the number of 
        ///     objects written to the underlying database.</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SyncObjectsStatePreCommit();
            var changesAsync = await base.SaveChangesAsync(cancellationToken);
            SyncObjectsStatePostCommit();

            return changesAsync;
        }

        private void SyncObjectsStatePreCommit()
        {
            foreach (var dbEntityEntry in ChangeTracker.Entries())
            {
                var entityObject = dbEntityEntry.Entity as IObjectState;
                if (entityObject != null)
                {
                    var objectState = entityObject.ObjectState;
                    if (dbEntityEntry.State != EntityState.Modified || objectState != ObjectState.Unchanged)
                        dbEntityEntry.State = StateHelper.ConvertState(objectState);
                }
            }
        }
    }
}
