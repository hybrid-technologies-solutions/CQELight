using CQELight.DAL.Common;
using CQELight.DAL.MongoDb.Mapping;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CQELight.DAL.MongoDb.Extensions;
using CQELight.Abstractions.DAL.Interfaces;

namespace CQELight.DAL.MongoDb.Adapters
{
    /// <summary>
    /// Data-reading adapter to use with MongoDb.
    /// </summary>
    public class MongoDataReaderAdapter : DisposableObject, IDataReaderAdapter
    {
        #region Ctor

        /// <summary>
        /// Creates a new instance of <see cref="MongoDataReaderAdapter"/>.
        /// </summary>
        public MongoDataReaderAdapter()
        {
            if (MongoDbContext.MongoClient == null)
            {
                throw new InvalidOperationException("MongoDbClient hasn't been initialized yet. Please, ensure that you've bootstrapped extension before attempting creating a new instance of MongoDataWriterAdapter");
            }
        }

        #endregion

        #region IDataReaderAdapter

#if NETSTANDARD2_0
        public IAsyncEnumerable<T> GetAsync<T>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, object>>? orderBy = null, bool includeDeleted = false)
#elif NETSTANDARD2_1 || NET5_0
        public async IAsyncEnumerable<T> GetAsync<T>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, object>>? orderBy = null, bool includeDeleted = false)
#endif
            where T : class
        {
            var collection = GetCollection<T>();
            FilterDefinition<T> whereFilter = FilterDefinition<T>.Empty;
            FilterDefinition<T> deletedFilter = FilterDefinition<T>.Empty;
            if (filter != null)
            {
                whereFilter = new FilterDefinitionBuilder<T>().Where(filter);
            }
            if (!includeDeleted && typeof(T).IsInHierarchySubClassOf(typeof(BasePersistableEntity)))
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
#elif NETSTANDARD2_1 || NET5_0
            foreach (var item in await (sortedResult ?? result).ToListAsync().ConfigureAwait(false))
            {
                yield return item;
            }
#endif
        }

        public async Task<T> GetByIdAsync<T>(object value) where T : class
        {
            var mappingInfo = MongoDbMapper.GetMapping<T>();
            if (string.IsNullOrWhiteSpace(mappingInfo.IdProperty) && mappingInfo.IdProperties?.Any() == false)
            {
                throw new InvalidOperationException($"The id field(s) for type {typeof(T).AssemblyQualifiedName} " +
                    $"has not been defined to allow searching by id." +
                    " You should either search with GetAsync method. You can also define id for this type by creating an 'Id' property (whatever type)" +
                    " or mark one property as id with the [PrimaryKey] attribute. Finally, you could define complex key with the [ComplexKey]" +
                    " attribute on top of your class to define multiple properties as part of the key.");
            }
            var collection = GetCollection<T>();
            var filter = value.GetIdFilterFromIdValue<T>(typeof(T));
            var data = await collection.FindAsync(filter).ConfigureAwait(false);
            return data.FirstOrDefault();
        }

        #endregion

        #region Private methods

        private IMongoCollection<T> GetCollection<T>()
            where T : class
        {
            var mappingInfo = MongoDbMapper.GetMapping<T>();
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

        #endregion
    }
}
