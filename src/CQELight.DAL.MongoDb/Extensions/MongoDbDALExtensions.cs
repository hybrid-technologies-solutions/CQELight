using CQELight.DAL.Attributes;
using CQELight.DAL.Common;
using CQELight.DAL.MongoDb.Mapping;
using CQELight.Tools.Extensions;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Reflection;

namespace CQELight.DAL.MongoDb.Extensions
{
    internal static class MongoDbDALExtensions
    {
        public static FilterDefinition<T> GetIdFilterFromIdValue<T>(this object idValue, Type entityType)
              where T : class
        {
            var mappingInfo = MongoDbMapper.GetMapping(entityType);
            var filter = FilterDefinition<T>.Empty;
            var filterBuilder = Builders<T>.Filter;
            if (entityType.IsInHierarchySubClassOf(typeof(PersistableEntity)))
            {
                filter = filterBuilder.Eq(nameof(PersistableEntity.Id), idValue);
            }
            else
            {
                if (entityType.IsInHierarchySubClassOf(typeof(ComposedKeyPersistableEntity))
                    || entityType.IsDefined(typeof(ComposedKeyAttribute)))
                {
                    var idValueProperties = idValue.GetType().GetAllProperties();
                    if (mappingInfo.IdProperties.Any(p => !idValueProperties.Any(pr => pr.Name == p)))
                    {
                        throw new InvalidOperationException("Provided id value is incomplete and cannot be used to search within database. " +
                            $"{entityType.Name}'s id required following fields : {string.Join(",", mappingInfo.IdProperties)}");
                    }
                    foreach (var item in idValueProperties)
                    {
                        filter &= filterBuilder.Eq(item.Name, item.GetValue(idValue));
                    }
                }
                else
                {
                    filter = filterBuilder.Eq(mappingInfo.IdProperty, idValue);
                }
            }

            return filter;
        }
    }
}
