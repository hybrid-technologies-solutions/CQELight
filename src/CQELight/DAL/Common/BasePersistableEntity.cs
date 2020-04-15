using CQELight.DAL.Attributes;
using CQELight.DAL.Interfaces;
using System;
using System.ComponentModel;

namespace CQELight.DAL.Common
{
    /// <summary>
    /// Base class definition for entity to be managed by database.
    /// </summary>
    public abstract class BasePersistableEntity : IPersistableEntity
    {
        #region CONSTS

        /// <summary>
        /// Name of column that holds deletion information.
        /// </summary>
        public const string CONST_DELETED_COLUMN = "Deleted";
        /// <summary>
        /// Name of column that holds deletion date.
        /// </summary>
        public const string CONST_DELETE_DATE_COLUMN = "DeleteDate";
        /// <summary>
        /// Name of column that holds edition date.
        /// </summary>
        public const string CONST_EDIT_DATE_COLUMN = "EditDate";

        #endregion

        #region Properties

        /// <summary>
        /// Last edit date of the object.
        /// </summary>
        [Column(CONST_EDIT_DATE_COLUMN)]
        public DateTime EditDate { get; set; }
        /// <summary>
        /// Soft deletion flag.
        /// </summary>
        [Column(CONST_DELETED_COLUMN), DefaultValue(false)]
        public bool Deleted { get; set; }
        /// <summary>
        /// Soft deletion date.
        /// </summary>
        [Column(CONST_DELETE_DATE_COLUMN), DefaultValue(null)]
        public DateTime? DeletionDate { get; set; }

        #endregion

        #region Abstract methods

        /// <summary>
        /// Check if key is defined or not. 
        /// </summary>
        /// <returns>True if key is defined, false otherwise.</returns>
        public abstract bool IsKeySet();

        /// <summary>
        /// Get key value.
        /// </summary>
        /// <returns>Value of the key boxed in a object.</returns>
        public abstract object? GetKeyValue();

        #endregion

    }
}
