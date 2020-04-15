using System;

namespace CQELight.DAL.Attributes
{
    /// <summary>
    /// Attribute that defines a proprety as primary key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// Name of the PK column.
        /// </summary>
        public string KeyName { get; private set; } = "";

        #endregion

        #region Ctor

        /// <summary>
        /// Create a key and use default name.
        /// </summary>
        public PrimaryKeyAttribute()
        {
        }

        /// <summary>
        /// Create a key attribute and use specific name.
        /// </summary>
        /// <param name="keyName">Name to use</param>
        public PrimaryKeyAttribute(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                throw new ArgumentNullException(nameof(keyName));
            }
            KeyName = keyName;
        }

        #endregion
    }
}
