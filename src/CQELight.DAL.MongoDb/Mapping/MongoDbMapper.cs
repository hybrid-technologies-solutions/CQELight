using System;
using System.Collections.Concurrent;
using System.Linq;

namespace CQELight.DAL.MongoDb.Mapping
{
    internal static class MongoDbMapper
    {
        #region Static properties

        private static readonly ConcurrentBag<MappingInfo> _mappings = new ConcurrentBag<MappingInfo>();

        #endregion

        #region Static methods

        public static MappingInfo GetMapping<T>()
            => GetMapping(typeof(T));

        public static MappingInfo GetMapping(Type t)
        {
            var mapping = _mappings.FirstOrDefault(m => m.EntityType == t);
            if (mapping == null)
            {
                mapping = new MappingInfo(t);
            }
            return mapping;
        }

        #endregion

    }
}
