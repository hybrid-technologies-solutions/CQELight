using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.DAL.Attributes
{
    /// <summary>
    /// Different kind of naviguation through object.
    /// </summary>
    [Flags]
#pragma warning disable S2342 // Enumeration types should comply with a naming convention
    public enum NavigationMode
#pragma warning restore S2342 // Enumeration types should comply with a naming convention
    {
        /// <summary>
        /// Creation mode.
        /// </summary>
        Create = 1 << 0,
        /// <summary>
        /// Update mode.
        /// </summary>
        Update = 1 << 1,
        /// <summary>
        /// All mode.
        /// </summary>
        All = Create | Update
    }

    /// <summary>
    /// Attribute use to prevents for seeking for différences when tracking changes on an object graph.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    [Obsolete("This attribute is no longer considered.")]
    public class NotNaviguableAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// Kind of naviguation concerned.
        /// </summary>
        public NavigationMode Mode { get; set; }

        #endregion

        #region Ctor
        /// <summary>
        /// Forbid to naviguate through some members when tracking changes on an object graph.
        /// </summary>
        /// <param name="mode">Specific mode.</param>
        public NotNaviguableAttribute(NavigationMode mode = NavigationMode.All)
        {
            Mode = mode;
        }

        #endregion

    }
}
