using CQELight.DAL.Attributes;
using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using CQELight.DAL.MongoDb.Mapping;
using CQELight.Tools.Extensions;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace CQELight.DAL.MongoDb
{
    [Obsolete("MongoRepository has been deprecated. Use DatabaseRepository instead.")]
    public class MongoRepository<T> : IDatabaseRepository<T>
        where T : class, IPersistableEntity
    {
        #region Members

        private readonly MappingInfo _mappingInfo;
        private ConcurrentBag<T> _toInsert = new ConcurrentBag<T>();
        private ConcurrentBag<T> _toUpdate = new ConcurrentBag<T>();
        private ConcurrentBag<T> _physicalToDelete = new ConcurrentBag<T>();

        #endregion

        #region Properties

        protected MongoClient MongoClient => MongoDbContext.MongoClient;

        #endregion

        #region Ctor

        public MongoRepository()
        {
            _mappingInfo = MongoDbMapper.GetMapping<T>();
        }

        #endregion

        #region Private methods

        private IMongoCollection<T> GetCollection()
        {
            var collection = MongoDbContext
                  .MongoClient
                  .GetDatabase(_mappingInfo.DatabaseName)
                  .GetCollection<T>(_mappingInfo.CollectionName);
            foreach (var item in _mappingInfo.Indexes)
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

        #endregion

        #region IDatabaseRepository methods

        public void Dispose()
        {
        }

#if NETSTANDARD2_0
        public IAsyncEnumerable<T> GetAsync(Expression<Func<T, bool>>? filter = null,
                                            Expression<Func<T, object>>? orderBy = null,
                                            bool includeDeleted = false,
                                            params Expression<Func<T, object>>[] includes)
#elif NETSTANDARD2_1
        public async IAsyncEnumerable<T> GetAsync(Expression<Func<T, bool>>? filter = null,
                                            Expression<Func<T, object>>? orderBy = null,
                                            bool includeDeleted = false,
                                            params Expression<Func<T, object>>[] includes)
#endif
        {
            var collection = GetCollection();
            FilterDefinition<T> whereFilter = FilterDefinition<T>.Empty;
            FilterDefinition<T> deletedFilter = FilterDefinition<T>.Empty;
            if (filter != null)
            {
                whereFilter = new FilterDefinitionBuilder<T>().Where(filter);
            }
            if (!includeDeleted && typeof(T).IsInHierarchySubClassOf(typeof(PersistableEntity)))
            {
                deletedFilter = new FilterDefinitionBuilder<T>().Eq("Deleted", false);
            }
            var result = collection
                .Find(new FilterDefinitionBuilder<T>().And(whereFilter, deletedFilter));
            IOrderedFindFluent<T, T>? sortedResult = null;
            if (orderBy != null)
            {
                sortedResult = result.SortBy(orderBy);
            }

#if NETSTANDARD2_0
            return (sortedResult ?? result)
                .ToEnumerable()
                .ToAsyncEnumerable();
#elif NETSTANDARD2_1
            foreach (var item in await (sortedResult ?? result).ToListAsync().ConfigureAwait(false))
            {
                yield return item;
            }
#endif
        }

        public async Task<T> GetByIdAsync<TId>(TId value)
            where TId : notnull
        {
            if (string.IsNullOrWhiteSpace(_mappingInfo.IdProperty) && _mappingInfo.IdProperties?.Any() == false)
            {
                throw new InvalidOperationException($"The id field(s) for type {typeof(T).AssemblyQualifiedName} " +
                    $"has not been defined to allow searching by id." +
                    " You should either search with GetAsync method. You can also define id for this type by creating an 'Id' property (whatever type)" +
                    " or mark one property as id with the [PrimaryKey] attribute. Finally, you could define complex key with the [ComplexKey]" +
                    " attribute on top of your class to define multiple properties as part of the key.");
            }
            var collection = GetCollection();
            FilterDefinition<T> filter = GetIdFilterFromIdValue(value);
            var data = await collection.FindAsync(filter).ConfigureAwait(false);
            return data.FirstOrDefault();
        }

        public void MarkForDelete(T entityToDelete, bool physicalDeletion = false)
        {
            if (entityToDelete is BasePersistableEntity bpe)
            {
                if (physicalDeletion)
                {
                    _physicalToDelete.Add(entityToDelete);
                }
                else
                {
                    bpe.Deleted = true;
                    bpe.DeletionDate = DateTime.Now;
                    _toUpdate.Add(entityToDelete);
                }
            }
            else
            {
                if (physicalDeletion)
                {
                    _physicalToDelete.Add(entityToDelete);
                }
                else
                {
                    throw new InvalidOperationException("Soft deletion of custom entities are not supported yet.");
                }
            }
        }

        public void MarkForDeleteRange(IEnumerable<T> entitiesToDelete, bool physicalDeletion = false)
            => entitiesToDelete.DoForEach(e => MarkForDelete(e, physicalDeletion));

        public void MarkForInsert(T entity)
        {
            if (entity is BasePersistableEntity bpe)
            {
                bpe.EditDate = DateTime.Now;
            }
            _toInsert.Add(entity);
        }

        public void MarkForInsertRange(IEnumerable<T> entities)
            => entities.DoForEach(MarkForInsert);

        public void MarkForUpdate(T entity)
        {
            if (entity is BasePersistableEntity bpe)
            {
                bpe.EditDate = DateTime.Now;
            }
            _toUpdate.Add(entity);
        }

        public void MarkForUpdateRange(IEnumerable<T> entities)
            => entities.DoForEach(MarkForUpdate);

        public void MarkIdForDelete<TId>(TId id, bool physicalDeletion = false)
            where TId : notnull
        {
            var entity = GetByIdAsync(id).GetAwaiter().GetResult();
            MarkForDelete(entity, physicalDeletion);
        }

        public async Task<int> SaveAsync()
        {
            using (var session = await MongoDbContext.MongoClient.StartSessionAsync().ConfigureAwait(false))
            {
                var actions = _toInsert.Count + _physicalToDelete.Count + _toUpdate.Count;
                var collection = GetCollection();

                session.StartTransaction();
                try
                {
                    if (_toInsert.Count > 0)
                    {
                        await collection.InsertManyAsync(_toInsert).ConfigureAwait(false);
                    }
                    if (_toUpdate.Count > 0)
                    {
                        foreach (var item in _toUpdate)
                        {
                            var idFilter = GetIdFilterFromIdValue(item.GetKeyValue());

                            var result = await collection.ReplaceOneAsync(idFilter, item).ConfigureAwait(false);
                            if (result.ModifiedCount == 0)
                            {
                                await collection.InsertOneAsync(item).ConfigureAwait(false);
                            }
                        }
                    }
                    if (_physicalToDelete.Count > 0)
                    {
                        var deletionFilter = FilterDefinition<T>.Empty;
                        foreach (var item in _physicalToDelete)
                        {
                            deletionFilter &= GetIdFilterFromIdValue(item.GetKeyValue());
                        }
                        await collection.DeleteManyAsync(deletionFilter).ConfigureAwait(false);
                    }
                    await session.CommitTransactionAsync().ConfigureAwait(false);
                    _toInsert = new ConcurrentBag<T>();
                    _physicalToDelete = new ConcurrentBag<T>();
                    _toUpdate = new ConcurrentBag<T>();
                }
                catch
                {
                    await session.AbortTransactionAsync().ConfigureAwait(false);
                }

                return actions;
            }
        }

        #endregion

        #region Private methods

        private FilterDefinition<T> GetIdFilterFromIdValue<TId>(TId value)
            where TId : notnull
        {
            var filterBuilder = Builders<T>.Filter;
            FilterDefinition<T> filter = FilterDefinition<T>.Empty;
            if (typeof(T).IsInHierarchySubClassOf(typeof(PersistableEntity)))
            {
                filter = filterBuilder.Eq(nameof(PersistableEntity.Id), value);
            }
            else
            {
                if (typeof(T).IsInHierarchySubClassOf(typeof(ComposedKeyPersistableEntity))
                    || typeof(T).IsDefined(typeof(ComposedKeyAttribute)))
                {
                    var idValueProperties = value.GetType().GetAllProperties();
                    if (_mappingInfo.IdProperties.Any(p => !idValueProperties.Any(pr => pr.Name == p)))
                    {
                        throw new InvalidOperationException("Provided id value is incomplete and cannot be used to search within database. " +
                            $"{typeof(T).Name}'s id required following fields : {string.Join(",", _mappingInfo.IdProperties)}");
                    }
                    foreach (var item in idValueProperties)
                    {
                        filter &= filterBuilder.Eq(item.Name, item.GetValue(value));
                    }
                }
                else
                {
                    filter = filterBuilder.Eq(_mappingInfo.IdProperty, value);
                }
            }

            return filter;
        }

        #endregion
    }
}
