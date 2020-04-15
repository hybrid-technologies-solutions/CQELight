using CQELight.Tools.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CQELight.Tools.Serialisation
{
    /// <summary>
    /// Json serialisation contract resolver.
    /// </summary>
    public class JsonSerialisationContractResolver : BaseJsonContractResolver
    {
        #region Static properties

        /// <summary>
        /// Default parameters.
        /// </summary>
        public static JsonSerializerSettings DefaultSerializeSettings { get; private set; }
            = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new JsonSerialisationContractResolver(true)
            };

        #endregion

        #region Ctor

        public JsonSerialisationContractResolver(params IJsonContractDefinition[] contracts)
            : base(contracts)
        {
        }

        public JsonSerialisationContractResolver(bool autoLoadContracts = false)
            : base(autoLoadContracts)
        {
        }

        #endregion

        #region Overriden methods

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Select(p => CreateProperty(p, memberSerialization))
                        .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(f => !f.Name.Contains("k__BackingField"))
                            .Select(f => CreateProperty(f, memberSerialization)))
                        .ToList();
            props.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (contracts?.Any() == true)
            {
                if (member is PropertyInfo || member is FieldInfo)
                {
                    foreach (var contract in contracts.ToList())
                    {
                        contract.SetSerialisationPropertyContractDefinition(property, member);
                    }
                }
                else
                {
                    property.ShouldSerialize = i => false;
                }
            }
            return property;
        }

        #endregion

    }
}
