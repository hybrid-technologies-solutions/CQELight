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
    /// Contrat de déserialisation Json.
    /// </summary>
    public class JsonDeserialisationContractResolver : BaseJsonContractResolver
    {
        #region Static properties

        /// <summary>
        /// Default settings.
        /// </summary>
        public static JsonSerializerSettings DefaultDeserializeSettings { get; private set; }
            = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new JsonDeserialisationContractResolver()
            };

        #endregion

        #region Ctor

        public JsonDeserialisationContractResolver(params IJsonContractDefinition[] contracts)
            : base(contracts)
        {
        }

        public JsonDeserialisationContractResolver(bool autoLoadContracts = false)
            :base(autoLoadContracts)
        {
        }

        #endregion

        #region Overriden methods

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Select(p => (p.SetMethod != null, CreateProperty(p, memberSerialization)))
                        .Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => !f.Name.Contains("k__BackingField"))
                        .Select(f => (true, CreateProperty(f, memberSerialization))))
                        .ToList();
            props.ForEach(p => { p.Item2.Writable = p.Item1; p.Item2.Readable = true; });
            return props.Select(p => p.Item2).ToList();
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (contracts?.Any() == true)
            {
                if ((member is PropertyInfo || member is FieldInfo) && !member.DeclaringType.IsInterface)
                {
                    foreach (var contract in contracts.ToList())
                    {
                        contract.SetDeserialisationPropertyContractDefinition(property, member);
                    }
                    if (property.ShouldDeserialize == null)
                    {
                        property.ShouldDeserialize = _ => (member as PropertyInfo)?.SetMethod != null;
                    }
                }
                else
                {
                    property.ShouldDeserialize = i => false;
                }
            }
            return property;
        }

        #endregion
    }
}
