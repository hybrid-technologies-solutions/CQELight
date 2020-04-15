using CQELight.Tools.Extensions;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.Tools.Serialisation
{
    /// <summary>
    /// Base class for Json contrat resolving.
    /// </summary>
    public abstract class BaseJsonContractResolver : DefaultContractResolver
    {
        #region Private static members

        private static readonly object s_lockObject = new object();

        protected static readonly IEnumerable<Type> s_AllContracts = ReflectionTools.GetAllTypes()
        .Where(m => m.GetInterfaces().Contains(typeof(IJsonContractDefinition))).ToList();
        private static readonly List<IJsonContractDefinition> s_IJsonContractDefinitionCache = new List<IJsonContractDefinition>();

        #endregion

        #region Members

        protected readonly IEnumerable<IJsonContractDefinition> contracts = Enumerable.Empty<IJsonContractDefinition>();

        #endregion

        #region Static methods

        private static IJsonContractDefinition? GetOrCreateInstance(Type type)
        {
            lock (s_lockObject)
            {
                IJsonContractDefinition? instance = s_IJsonContractDefinitionCache.Find(m => m.GetType() == type);
                if (instance == null)
                {
                    instance = type.CreateInstance() as IJsonContractDefinition;
                    if (instance != null)
                    {
                        s_IJsonContractDefinitionCache.Add(instance);
                    }
                }
                return instance;
            }
        }

        #endregion

        #region Ctor

        public BaseJsonContractResolver(IEnumerable<IJsonContractDefinition> contracts)
        {
            this.contracts = contracts;
        }

        public BaseJsonContractResolver(bool autoLoad)
        {
            if (autoLoad)
            {
                contracts = s_AllContracts.Select(GetOrCreateInstance).WhereNotNull().ToList();
            }
        }

        #endregion
    }
}
