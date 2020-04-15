using CQELight.DAL.Attributes;
using CQELight.DAL.Exceptions;
using CQELight.Tools.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace CQELight.DAL.Common
{
    /// <summary>
    /// Base class to manager composed key entities.
    /// </summary>
    public abstract class ComposedKeyPersistableEntity : BasePersistableEntity
    {
        #region Overriden methods

        /// <summary>
        /// Retrieve the composed key value.
        /// </summary>
        /// <returns>Composed key value.</returns>
        public override object? GetKeyValue()
        {
            var entityType = GetType();
            var composedKeyAttr = entityType.GetCustomAttribute<ComposedKeyAttribute>();
            if (composedKeyAttr != null)
            {
                var keyProps = entityType.GetAllProperties().Where(p => composedKeyAttr.PropertyNames.Contains(p.Name));
                if (keyProps != null)
                {
                    var values = keyProps.Select(p => p.GetValue(this)).WhereNotNull();
                    if (values?.Any() == true)
                    {
                        return string.Join(",", keyProps.Select(p => p.GetValue(this)));
                    }
                    return null;
                }
            }
            throw new ComposedKeyAttributeNotDefinedException(entityType);
        }

        /// <summary>
        /// Gets if the key is set or not.
        /// </summary>
        /// <returns>True if key is defined, false otherwise.</returns>
        public override bool IsKeySet()
        {
            var entityType = GetType();
            var composedKeyAttr = entityType.GetCustomAttribute<ComposedKeyAttribute>();
            if (composedKeyAttr != null)
            {
                var keyProps = entityType.GetAllProperties().Where(p => composedKeyAttr.PropertyNames.Contains(p.Name));
                if (keyProps != null)
                {
                    foreach (var key in keyProps)
                    {
                        object? defaultValue = null;
                        if (key.PropertyType.IsValueType)
                        {
                            defaultValue = key.PropertyType.CreateInstance();
                        }
                        if (key.GetValue(this) == defaultValue)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            throw new ComposedKeyAttributeNotDefinedException(entityType);
        }

        #endregion

    }
}
