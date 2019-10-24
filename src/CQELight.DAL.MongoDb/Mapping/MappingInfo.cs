using CQELight.DAL.Attributes;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CQELight.DAL.MongoDb.Mapping
{
    internal class MappingInfo
    {
        #region Members

        private ILogger _logger;
        private List<PropertyInfo> _properties;
        private List<IndexDetail> _indexes = new List<IndexDetail>();

        #endregion

        #region Properties

        public Type EntityType { get; private set; }
        public string CollectionName { get; private set; }
        public string DatabaseName { get; private set; }
        public string IdProperty { get; private set; }
        public IEnumerable<string> IdProperties { get; internal set; }
        public IEnumerable<IndexDetail> Indexes => _indexes.AsEnumerable();

        #endregion

        #region Ctor

        public MappingInfo(Type type)
            : this(type, null)
        {
        }

        public MappingInfo(Type type, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
                loggerFactory.AddProvider(new DebugLoggerProvider());
            }
            _properties = type.GetAllProperties().ToList();
            _logger = loggerFactory.CreateLogger("CQELight.DAL.MongoDb");
            EntityType = type;
            ExtractInformationsFromType();
        }

        #endregion

        #region Private methods

        private void Log(string message)
            => _logger.LogInformation(() => message);

        private void ExtractInformationsFromType()
        {
            ExtractTableAndCollectionInformations();

            var composedKeyAttribute = EntityType.GetCustomAttribute<ComposedKeyAttribute>();
            if (composedKeyAttribute == null)
            {
                var idProperty = _properties.FirstOrDefault(p =>
                    p.IsDefined(typeof(PrimaryKeyAttribute)) || p.Name == "Id");
                if (idProperty != null)
                {
                    IdProperty = idProperty.Name;
                }
            }
            else
            {
                IdProperties = composedKeyAttribute.PropertyNames;
            }
            ExtractSimpleIndexInformations();
            ExtractComplexIndexInformations();
        }

        private void ExtractComplexIndexInformations()
        {
            var definedComplexIndexes = EntityType.GetCustomAttributes<ComplexIndexAttribute>().ToList();
            for (int i = 0; i < definedComplexIndexes.Count; i++)
            {
                var complexIndexDefinition = definedComplexIndexes[i];
                _indexes.Add(new IndexDetail
                {
                    Properties = complexIndexDefinition.PropertyNames
                });
            }
        }

        private void ExtractSimpleIndexInformations()
        {
            foreach (var prop in _properties)
            {
                var indexAttribute = prop.GetCustomAttribute<IndexAttribute>();
                if (indexAttribute != null)
                {
                    _indexes.Add(new IndexDetail
                    {
                        Properties = new[] { prop.Name },
                        Unique = indexAttribute.IsUnique
                    });
                }
            }
        }

        private void ExtractTableAndCollectionInformations()
        {
            Log($"Beginning of treatment for type '{CollectionName}'");

            var tableAttr = EntityType.GetCustomAttribute<TableAttribute>();
            CollectionName =
                !string.IsNullOrWhiteSpace(tableAttr?.TableName)
                ? tableAttr.TableName
                : EntityType.Name;
            if (!string.IsNullOrWhiteSpace(tableAttr?.SchemaName))
            {
                DatabaseName = tableAttr.SchemaName;
            }
            else
            {
                DatabaseName = "DefaultDatabase";
            }
            Log($"Type '{CollectionName}' is defined to go in database '{DatabaseName}'");
        }

        #endregion

    }
}
