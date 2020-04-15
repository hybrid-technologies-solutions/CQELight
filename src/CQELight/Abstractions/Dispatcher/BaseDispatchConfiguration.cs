using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.Abstractions.Dispatcher.Configuration
{
    /// <summary>
    /// Base class for dispatching configuration.
    /// </summary>
    public abstract class BaseDispatchConfiguration
    {
        #region Properties

        /// <summary>
        /// Instance of serializer.
        /// </summary>
        public IDispatcherSerializer? Serializer { get; set; }

        /// <summary>
        /// Handler to fire if error.
        /// </summary>
        public Action<Exception>? ErrorHandler { get; set; }

        /// <summary>
        /// Collection of buses types for dispatch.
        /// </summary>
        public IEnumerable<Type> BusesTypes { get; set; } = Enumerable.Empty<Type>();

        /// <summary>
        /// Flag that indicates if object is security critical.
        /// </summary>
        public bool IsSecurityCritical { get; set; }

        #endregion

    }
}
