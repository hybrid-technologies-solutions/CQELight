using CQELight.DAL.Attributes;
using CQELight.DAL.Exceptions;
using CQELight.Tools.Extensions;
using System.Linq;
using System.Reflection;

namespace CQELight.DAL.Common
{
    /// <summary>
    /// Base class for entity that have a custom primary key.
    /// </summary>
    public abstract class CustomKeyPersistableEntity : BasePersistableEntity
    {
        #region Private members

        private PropertyInfo? _primaryKeyProperty;

        #endregion

        #region Private properties

        private PropertyInfo PrimaryKeyProperty
        {
            get
            {
                if (_primaryKeyProperty == null)
                {
                    var entityType = GetType();
                    _primaryKeyProperty = entityType.GetAllProperties().SingleOrDefault(p => p.IsDefined(typeof(PrimaryKeyAttribute), true));
                }
                return _primaryKeyProperty;
            }
        }

        #endregion

        #region Overriden methods

        /// <summary>
        /// Retrieves the custom key value.
        /// </summary>
        /// <returns>Custom key value.</returns>
        public override object GetKeyValue()
        {
            if (PrimaryKeyProperty != null)
            {
                return PrimaryKeyProperty.GetValue(this);
            }
            throw new PrimaryKeyPropertyNotFoundException(GetType());
        }

        /// <summary>
        /// Get if the key is set or not.
        /// </summary>
        /// <returns>True if key is set, false otherwise.</returns>
        public override bool IsKeySet()
        {
            if (PrimaryKeyProperty != null)
            {
                object? defaultValue = null;
                if (PrimaryKeyProperty.PropertyType.IsValueType)
                {
                    defaultValue = PrimaryKeyProperty.PropertyType.CreateInstance();
                }
                return PrimaryKeyProperty.GetValue(this) != defaultValue;
            }
            throw new PrimaryKeyPropertyNotFoundException(GetType());
        }

        #endregion

    }
}
