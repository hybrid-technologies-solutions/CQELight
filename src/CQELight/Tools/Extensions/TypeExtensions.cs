using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CQELight.Tools.Extensions
{
    /// <summary>
    /// Bunch of extensions methods for Type.
    /// </summary>
    public static class TypeExtensions
    {
        #region Members

        internal readonly static ConcurrentDictionary<Type, List<PropertyInfo>> PropertiesInfoCache
            = new ConcurrentDictionary<Type, List<PropertyInfo>>();

        internal readonly static ConcurrentDictionary<Type, List<FieldInfo>> FieldsInfoCache
            = new ConcurrentDictionary<Type, List<FieldInfo>>();

        #endregion

        #region Public static methods

        /// <summary>
        /// Check if a type is in hierarchy of a parent type.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="parent">Other to check if in hierarchy.</param>
        /// <returns>True if parent type or greater parent type, false otherwise.</returns>
        public static bool IsInHierarchySubClassOf(this Type type, Type parent)
        {
            if (type == parent || type.IsSubclassOf(parent))
            {
                return true;
            }
            if (type == typeof(object) || type.BaseType == null)
            {
                return false;
            }
            return type.BaseType.IsInHierarchySubClassOf(parent);
        }

        /// <summary>
        /// Creates an instance of a type by reflection.
        /// </summary>
        /// <param name="type">Type of object expected.</param>
        /// <param name="parameters">Parameters that are needed to create an instance.</param>
        /// <returns>Type instance.</returns>
        public static object? CreateInstance(this Type type, params object[] parameters)
        {
            var ctor = Array.Find(type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), m => m.GetParameters().Length == parameters.Length);
            if (ctor != null)
            {
                return ctor.Invoke(parameters);
            }
            return null;
        }

        /// <summary>
        /// Creates an instance of a type by reflection.
        /// </summary>
        /// <param name="parameters">Parameters that are needed to create an instance.</param>
        /// <param name="type">Type of object expected.</param>
        /// <returns>Type instance.</returns>
        /// <typeparam name="T">Type of object you want.</typeparam>
        public static T? CreateInstance<T>(this Type type, params object[] parameters) where T : class
        {
            var ctor = Array.Find(typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), m => m.GetParameters().Length == parameters.Length);
            if (ctor != null)
            {
                return (T)ctor.Invoke(parameters);
            }
            return null;
        }

        /// <summary>
        /// Check if type implements at least one the generic interface.
        /// </summary>
        /// <param name="typeToCheck">Type to check.</param>
        /// <param name="genericInterfaceType">Interface's type.</param>
        /// <returns>VTrue if type implements it, false otherwise.</returns>
        public static bool ImplementsRawGenericInterface(this Type typeToCheck, Type genericInterfaceType)
            => typeToCheck.GetInterfaces().Any(m => m.IsGenericType && m.GetGenericTypeDefinition() == genericInterfaceType);

        /// <summary>
        /// Get all properties for a specific type.
        /// </summary>
        /// <param name="type">Type on which we want all properties.</param>
        /// <returns>Update collection of properties infos.</returns>
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            if (!PropertiesInfoCache.TryGetValue(type, out List<PropertyInfo> properties))
            {
                properties = type.GetRuntimeProperties().ToList();
                PropertiesInfoCache.TryAdd(type, properties);
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding {type.Name} properties into cache. {properties.Count.ToString()} properties in total.");
                    System.Diagnostics.Debug.WriteLine($"Properties list : {string.Join(",", properties)}");
                }
            }
            return properties.AsEnumerable();
        }

        /// <summary>
        /// Get all members for a specific type.
        /// </summary>
        /// <param name="type">Type on which we want all members.</param>
        /// <returns>Collection of members info.</returns>
        public static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            if (!FieldsInfoCache.TryGetValue(type, out List<FieldInfo> fields))
            {
                fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
                FieldsInfoCache.TryAdd(type, fields);
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding {type.Name} fields into cache. {fields.Count.ToString()} fields in total.");
                    System.Diagnostics.Debug.WriteLine($"Fields list : {string.Join(",", fields)}");
                }
            }
            return fields.AsEnumerable();
        }

        /// <summary>
        /// Checks wether a specific type exists in type hierarchy.
        /// Searching is case insensitive and use exact match.
        /// </summary>
        /// <param name="type">Type to start from.</param>
        /// <param name="typeName">Name of the looking type.</param>
        /// <returns>True if typeName is found, false otherwise.</returns>
        public static bool NameExistsInHierarchy(this Type type, string typeName)
        {
            if(type.BaseType == typeof(object) || type == typeof(object))
            {
                return false;
            }
            var typeEquals = type.BaseType.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase);
            if(typeEquals)
            {
                return true;
            }
            return NameExistsInHierarchy(type.BaseType, typeName);
        }

        #endregion

    }
}
