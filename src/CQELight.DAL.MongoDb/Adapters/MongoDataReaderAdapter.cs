using CQELight.DAL.Attributes;
using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using CQELight.DAL.MongoDb.Mapping;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CQELight.DAL.MongoDb.Extensions;

namespace CQELight.DAL.MongoDb.Adapters
{
    class MongoDataReaderAdapter : DisposableObject, IDataReaderAdapter
    {
        #region IDataReaderAdapter

        public IAsyncEnumerable<T> GetAsync<T>(Expression<Func<T, bool>> filter = null, Expression<Func<T, object>> orderBy = null, bool includeDeleted = false)
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
            IOrderedFindFluent<T, T> sortedResult = null;
            if (orderBy != null)
            {
                sortedResult = result.SortBy(orderBy);
            }
            return (sortedResult ?? result)
                .ToEnumerable()
                .ToAsyncEnumerable();
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
                  .MongoClient
                  .GetDatabase(mappingInfo.DatabaseName)
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
