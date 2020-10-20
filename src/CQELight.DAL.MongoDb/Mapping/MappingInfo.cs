﻿using CQELight.DAL.Attributes;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CQELight.DAL.MongoDb.Mapping
{
    internal class MappingInfo
    {
        #region Members

        private readonly ILogger _logger;
        private readonly List<PropertyInfo> _properties;
        internal readonly List<IndexDetail> _indexes = new List<IndexDetail>();

        #endregion

        #region Properties

        public Type EntityType { get; }
        public string? CollectionName { get; internal set; }
        [Obsolete("Database must be configured to configuration level, not entity level")]
        public string? DatabaseName { get; private set; }
        public string? IdProperty { get; internal set; }
        public IEnumerable<string>? IdProperties { get; internal set; }
        public IEnumerable<IndexDetail> Indexes => _indexes.AsEnumerable();

        #endregion

        #region Ctor

        public MappingInfo(Type type)
            : this(type, new LoggerFactory(new[] { new DebugLoggerProvider() }))
        {
        }

        public MappingInfo(Type type, ILoggerFactory loggerFactory)
        {
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
                var idProperty = _properties.Find(p =>
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
                ? tableAttr!.TableName
                : EntityType.Name;
            Log($"Entity of type {EntityType.FullName} for to Collection '{CollectionName}'");
            if (!string.IsNullOrWhiteSpace(tableAttr?.SchemaName))
            {
                DatabaseName = tableAttr!.SchemaName;
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
