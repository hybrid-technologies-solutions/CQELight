using CQELight.Abstractions.DAL.Interfaces;
using CQELight.DAL.Common;
using CQELight.DAL.MongoDb.Extensions;
using CQELight.DAL.MongoDb.Mapping;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.DAL.MongoDb.Adapters
{
    /// <summary>
    /// Data-writing adapter to use with MongoDb.
    /// </summary>
    public class MongoDataWriterAdapter : DisposableObject, IDataWriterAdapter
    {
        #region Members

        private IClientSessionHandle? session;
        private int actions = 0;
        private readonly SemaphoreSlim sessionLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim parallelSafety;
        private readonly ILogger<MongoDataWriterAdapter>? logger;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of <see cref="MongoDataWriterAdapter"/>.
        /// </summary>
        public MongoDataWriterAdapter(
            ILogger<MongoDataWriterAdapter>? logger = null)
        {
            if (MongoDbContext.MongoClient == null)
            {
                throw new InvalidOperationException("MongoDbClient hasn't been initialized yet. Please, ensure that you've bootstrapped extension before attempting creating a new instance of MongoDataWriterAdapter");
            }
            parallelSafety = new SemaphoreSlim(MongoDbContext.MongoClient.Settings.MaxConnectionPoolSize / 2);
            this.logger = logger;
        }

        #endregion

        #region Private methods

        private async Task CheckIfSessionIsStartedAsync()
        {
            if (session == null)
            {
                await sessionLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (session == null)
                    {
                        session = await MongoDbContext.MongoClient.StartSessionAsync();
                        if (MongoDbContext.MongoClient.Cluster.Description.Type != ClusterType.Standalone)
                        {
                            session.StartTransaction();
                        }
                    }
                }
                finally
                {
                    sessionLock.Release();
                }
            }
        }

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            try
            {
                session?.Dispose();
            }
            catch
            {
                //No need to throw on disposal
            }
            base.Dispose(disposing);
        }

        private IMongoCollection<T> GetCollection<T>(Type entityType)
            where T : class
        {
            var mappingInfo = MongoDbMapper.GetMapping(entityType);
            var collection = MongoDbContext
                 .Database
                 .GetCollection<T>(mappingInfo.CollectionName);
            foreach (var item in mappingInfo.Indexes)
            {
                if (item.Properties.Count() > 1)
                {
                    var indexKeyDefintion = Builders<T>.IndexKeys.Ascending(item.Properties.First());
                    foreach (var prop in item.Properties.Skip(1))
                    {
                        indexKeyDefintion = indexKeyDefintion.Ascending(prop);
                    }
                    collection.Indexes.CreateOne(new CreateIndexModel<T>(indexKeyDefintion));
                }
                else
                {
                    collection.Indexes.CreateOne(
                        new CreateIndexModel<T>(
                            Builders<T>.IndexKeys.Ascending(item.Properties.First())));
                }
            }
            return collection;
        }

        private object ExtractIdValue<T>(T entity)
            where T : class
        {
            var entityType = entity.GetType();
            var mappingInfo = MongoDbMapper.GetMapping(entityType);
            if (string.IsNullOrWhiteSpace(mappingInfo.IdProperty) && mappingInfo.IdProperties?.Any() == false)
            {
                throw new InvalidOperationException($"The id field(s) for type {typeof(T).AssemblyQualifiedName} " +
                    $"has not been defined to allow update/delete object." +
                    " You should write data by yourself. You can also define id for this type by creating an 'Id' property (whatever type)" +
                    " or mark one property as id with the [PrimaryKey] attribute. Finally, you could define complex key with the [ComplexKey]" +
                    " attribute on top of your class to define multiple properties as part of the key.");
            }
            if (!string.IsNullOrWhiteSpace(mappingInfo.IdProperty))
            {
                return entityType.GetAllProperties().First(p => p.Name == mappingInfo.IdProperty).GetValue(entity);
            }
            else
            {
                return entityType.GetAllProperties().Where(p => mappingInfo.IdProperties.Contains(p.Name)).Select(p => p.GetValue(entity)).ToArray();
            }
        }

        #endregion

        #region DataWriterAdapter

        public async Task DeleteAsync<T>(T entity, bool physicalDeletion) where T : class
        {
            if (physicalDeletion)
            {
                await CheckIfSessionIsStartedAsync();
                var entityType = entity.GetType();
                var deletionFilter = ExtractIdValue(entity).GetIdFilterFromIdValue<T>(entityType);
                try
                {
                    await parallelSafety.WaitAsync();
                    await GetCollection<T>(entityType).DeleteOneAsync(deletionFilter);
                    actions++;
                }
                finally
                {
                    parallelSafety.Release();
                }
            }
            else
            {
                if (entity is BasePersistableEntity basePersistableEntity)
                {
                    basePersistableEntity.Deleted = true;
                    basePersistableEntity.DeletionDate = DateTime.UtcNow;
                    await UpdateAsync(entity).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unable to perform soft deletion of object of type {typeof(T).FullName}. " +
                        "You should do it by yourself and update this object instead of deleting it.");
                }
            }
        }

        public async Task InsertAsync<T>(T entity) where T : class
        {
            await CheckIfSessionIsStartedAsync().ConfigureAwait(false);
            try
            {
                await parallelSafety.WaitAsync();
                await GetCollection<T>(entity.GetType()).InsertOneAsync(entity).ConfigureAwait(false);
                actions++;
            }
            finally
            {
                parallelSafety.Release();
            }
        }
        public async Task UpdateAsync<T>(T entity) where T : class
        {
            await CheckIfSessionIsStartedAsync().ConfigureAwait(false);
            var entityType = entity.GetType();
            var idValue = ExtractIdValue(entity);
            var idFilter = idValue.GetIdFilterFromIdValue<T>(entityType);

            try
            {
                await parallelSafety.WaitAsync();
                var result = await GetCollection<T>(entityType).ReplaceOneAsync(idFilter, entity);
                if (result.ModifiedCount == 0)
                {
                    logger?.LogWarning($"Entity of type {typeof(T).FullName} with id value {idValue} cannot be updated as it doesn't currently exists within database. Insert it instead of update it.");
                }
                actions += Convert.ToInt32(result.ModifiedCount);
            }
            finally
            {
                parallelSafety.Release();
            }
        }

        public async Task<int> SaveAsync()
        {
            if (MongoDbContext.MongoClient.Cluster.Description.Type != ClusterType.Standalone)
            {
                if (session != null) // nothing is writen
                {
                    try
                    {
                        await session.CommitTransactionAsync().ConfigureAwait(false);
                        return actions;
                    }
                    catch
                    {
                        await session.AbortTransactionAsync().ConfigureAwait(false);
                        return 0;
                    }
                }
            }
            return 0;
        }

        #endregion
    }
}
