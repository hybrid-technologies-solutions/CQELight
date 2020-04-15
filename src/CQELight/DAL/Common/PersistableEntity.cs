using CQELight.DAL.Attributes;
using CQELight.Tools.Extensions;
using System;

namespace CQELight.DAL.Common
{
    /// <summary>
    /// Base class for database entity that uses Guid as primary key.
    /// </summary>
    public abstract class PersistableEntity : BasePersistableEntity
    {
        #region Properties

        /// <summary>
        /// Identifiant de l'Entity.
        /// </summary>
        [PrimaryKey]
        public Guid Id { get; protected internal set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructeur par défaut.
        /// </summary>
        protected PersistableEntity()
        {
            Id = Guid.Empty;
        }

        #endregion

        #region Public methods

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (!this.SameTypeCheck(obj))
            {
                return false;
            }
            return ((PersistableEntity)obj).Id.Equals(Id);
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => Id.ToString().GetHashCode();

        /// <summary>
        /// Check if current Id is set, meaning that Id is not empty.
        /// </summary>
        /// <returns>True if Guid is set, false if empty.</returns>
        public override bool IsKeySet() => Id != Guid.Empty;

        /// <summary>
        /// Get the value of the id.
        /// </summary>
        /// <returns>Id as guid.</returns>
        public override object GetKeyValue() => Id;

        #endregion
    }
}
