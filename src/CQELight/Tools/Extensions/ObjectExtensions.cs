using CQELight.Tools.Serialisation;
using Newtonsoft.Json;
using System.Linq;

namespace CQELight.Tools.Extensions
{
    /// <summary>
    /// Class of extensions methods for objects.
    /// </summary>
    public static class ObjectExtensions
    {
        #region Public static methods

        /// <summary>
        /// Check if type are same.
        /// </summary>
        /// <param name="obj">Other objet instance to compare with.</param>
        /// <param name="value">Object instance.</param>
        /// <returns>If both object are same type.</returns>
        public static bool SameTypeCheck(this object value, object obj)
            => value?.GetType() == obj?.GetType();

        /// <summary>
        /// Retrieves Json data from an object.
        /// </summary>
        /// <param name="value">Objet which we want Json.</param>
        /// <returns>Json string if object is not null.</returns>
        public static string ToJson(this object value)
            => ToJson(value, settings: null);

        /// <summary>
        /// Retrieves Json data from an object.
        /// </summary>
        /// <param name="value">Objet which we want Json.</param>
        /// <param name="serializePrivateFields">Flag that indicates if private fields should be
        /// serialized or not.</param>
        /// <returns>Json string if object is not null.</returns>
        public static string ToJson(this object value, bool serializePrivateFields)
            => ToJson(value,

                new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    ContractResolver =
                        serializePrivateFields
                        ? new JsonSerialisationContractResolver(new AllFieldSerialisationContract())
                        : null
                });

        /// <summary>
        /// Retrieves Json data from an object, with some serialization contracts.
        /// </summary>
        /// <param name="value">Object to serialiaz</param>
        /// <param name="contracts">Collection of contracts to use.</param>
        /// <returns>Json string if object is not null.</returns>
        public static string ToJson(this object value, params IJsonContractDefinition[] contracts)
            => ToJson(value,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    ContractResolver = new JsonSerialisationContractResolver(contracts)
                });

        /// <summary>
        /// Retrieves Json data from an object.
        /// </summary>
        /// <param name="value">Objet which we want Json.</param>
        /// <param name="settings">Custom JsonSerializerSettings</param>
        /// <returns>Json string if object is not null.</returns>
        public static string ToJson(this object value, JsonSerializerSettings? settings)
        {
            if (value == null)
            {
                return string.Empty;
            }
            return JsonConvert.SerializeObject(value, settings ?? new JsonSerializerSettings
            {
                Formatting = Formatting.None
            });
        }

        /// <summary>
        /// Check if a specific instance is in a collection.
        /// </summary>
        /// <typeparam name="T">Type of value to search.</typeparam>
        /// <param name="value">Curent value to search.</param>
        /// <param name="params">Collection to search in.</param>
        /// <returns>True if value is inside the params collection, false otherwise.</returns>
        public static bool In<T>(this T value, params T[] @params)
        {
            if (@params == null)
            {
                return false;
            }
            if (!@params.Any())
            {
                return false;
            }
            return @params.Any(v => value?.Equals(v) == true);
        }

        #endregion

    }
}
