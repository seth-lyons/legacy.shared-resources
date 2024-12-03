using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharedResources
{
    public static class EFExtentions
    {
        enum OperationType { Insert, Update, Delete, Upsert, Sync }
        private static readonly BulkConfig _config = new BulkConfig { UseTempDB = true, BatchSize = 100000, BulkCopyTimeout = 0 };

        public static void Read<TContext, TEntity>(this TContext @this, IList<TEntity> entities, params string[] updateByProperties)
            where TEntity : class
            where TContext : DbContext
            => @this.BulkRead(entities, new BulkConfig { UpdateByProperties = updateByProperties.ToList() });

        public static async Task ReadAsync<TContext, TEntity>(this TContext @this, IList<TEntity> entities, params string[] updateByProperties)
            where TEntity : class
            where TContext : DbContext
            => await @this.BulkReadAsync(entities, new BulkConfig { UpdateByProperties = updateByProperties.ToList() });

        public static void Insert<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
            where TEntity : class
            where TContext : DbContext
            => Process(@this, entities, OperationType.Insert);

        public static async Task InsertAsync<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
            where TEntity : class
            where TContext : DbContext
            => await ProcessAsync(@this, entities, OperationType.Insert);

        public static void Update<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
             where TEntity : class
             where TContext : DbContext
             => Process(@this, entities, OperationType.Update);

        public static async Task UpdateAsync<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
            where TEntity : class
            where TContext : DbContext
            => await ProcessAsync(@this, entities, OperationType.Update);

        public static void Upsert<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
            where TEntity : class
            where TContext : DbContext
            => Process(@this, entities, OperationType.Upsert);

        public static async Task UpsertAsync<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
            where TEntity : class
            where TContext : DbContext
            => await ProcessAsync(@this, entities, OperationType.Upsert);

        public static void Sync<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
            where TEntity : class
            where TContext : DbContext
            => Process(@this, entities, OperationType.Sync);

        public static async Task SyncAsync<TContext, TEntity>(this TContext @this, IList<TEntity> entities)
            where TEntity : class
            where TContext : DbContext
            => await ProcessAsync(@this, entities, OperationType.Sync);

        public static void TruncateTable<TContext, TEntity>(this TContext @this)
            where TEntity : class
            where TContext : DbContext
            => @this.Truncate<TEntity>();

        public static async Task TruncateTableAsync<TContext, TEntity>(this TContext @this)
           where TEntity : class
           where TContext : DbContext
           => await @this.TruncateAsync<TEntity>();

        public static async Task SoftSyncAsync<TContext, TEntity>(this TContext @this, IList<TEntity> entities, string deletedFlagName = null)
            where TEntity : class
            where TContext : DbContext
        {
            await ProcessAsync(@this, entities, OperationType.Upsert);

            var isDeletedProperty = deletedFlagName != null
                 ? typeof(TEntity)
                     ?.GetProperties()
                     ?.FirstOrDefault(p => p.Name.Equals(deletedFlagName, StringComparison.InvariantCultureIgnoreCase))
                 : typeof(TEntity)
                     ?.GetProperties()
                     ?.FirstOrDefault(p => (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
                         && (p.Name.Equals("Deleted", StringComparison.InvariantCultureIgnoreCase)
                            || p.Name.Equals("IsDeleted", StringComparison.InvariantCultureIgnoreCase)
                            || p.Name.Equals("IsDeletedFlag", StringComparison.InvariantCultureIgnoreCase)));

            if (isDeletedProperty != null)
            {
                var entityType = @this?.Model?.FindEntityType(typeof(TEntity));
                var keyProperties = entityType
                    ?.FindPrimaryKey()
                    ?.Properties
                    ?.Select(p => p.PropertyInfo)
                    ?.ToList();

                var entityHashes = entities
                    ?.Select(e => string.Join("_", keyProperties.Select(kp => kp.GetValue(e).ToString())).GetHashCode())
                    ?.ToHashSet();

                var allEntities = @this
                    ?.Set<TEntity>()
                    ?.AsNoTracking()
                    ?.ToList();

                var leftOverEntities = allEntities
                    ?.Where(e => (bool)isDeletedProperty.GetValue(e) != true && !entityHashes.Contains(string.Join("_", keyProperties.Select(kp => kp.GetValue(e).ToString())).GetHashCode()))
                    ?.ToList();

                if (leftOverEntities?.Any() == true)
                {
                    leftOverEntities?.ForEach(e => isDeletedProperty.SetValue(e, true));
                    await ProcessAsync(@this, leftOverEntities, OperationType.Update);
                }
            }
        }

        private static async Task ProcessAsync<TContext, TEntity>(TContext context, IList<TEntity> entities, OperationType operation)
            where TEntity : class
            where TContext : DbContext
        {
            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                if (operation == OperationType.Insert)
                    await context.BulkInsertAsync(entities, _config);
                else if (operation == OperationType.Update)
                    await context.BulkUpdateAsync(entities, _config);
                else if (operation == OperationType.Delete)
                    await context.BulkDeleteAsync(entities, _config);
                else if (operation == OperationType.Upsert)
                    await context.BulkInsertOrUpdateAsync(entities, _config);
                else if (operation == OperationType.Sync)
                    await context.BulkInsertOrUpdateOrDeleteAsync(entities, _config);
                await transaction.CommitAsync();
            }
        }

        private static void Process<TContext, TEntity>(TContext context, IList<TEntity> entities, OperationType operation)
            where TEntity : class
            where TContext : DbContext
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                if (operation == OperationType.Insert)
                    context.BulkInsert(entities, _config);
                else if (operation == OperationType.Update)
                    context.BulkUpdate(entities, _config);
                else if (operation == OperationType.Delete)
                    context.BulkDelete(entities, _config);
                else if (operation == OperationType.Upsert)
                    context.BulkInsertOrUpdate(entities, _config);
                else if (operation == OperationType.Sync)
                    context.BulkInsertOrUpdateOrDelete(entities, _config);
                transaction.Commit();
            }
        }

        private static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> source,
            IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }
    }
}
