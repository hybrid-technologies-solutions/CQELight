using CQELight.Abstractions.Dispatcher.Configuration;
using System;

namespace CQELight.Dispatcher.Configuration.Internal
{
    /// <summary>
    /// Internal class to help managing configuration to build.
    /// </summary>
    internal class EventDispatchConfiguration : BaseDispatchConfiguration
    {
        #region Properties

        /// <summary>
        /// Type of event configuration is about.
        /// </summary>
        public Type EventType { get; set; }

        #endregion

        #region Ctor

        public EventDispatchConfiguration(Type eventType)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        }

        #endregion
    }
}
