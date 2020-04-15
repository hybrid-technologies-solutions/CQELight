using CQELight.Abstractions.EventStore.Interfaces;
using System;
using System.Collections.Generic;

namespace CQELight.EventStore
{
    /// <summary>
    /// Basic implementation of snapshot provider that holds
    /// a mapping dictionary of types and snapshot behavior provider.
    /// </summary>
    public class BasicSnapshotBehaviorProvider : ISnapshotBehaviorProvider
    {
        #region Members

        /// <summary>
        /// Dictionary of mapping.
        /// </summary>
        private readonly Dictionary<Type, ISnapshotBehavior> _configuration;

        #endregion

        #region Ctor

        /// <summary>
        /// Creation of a new snapshot behavior provider, based on a already
        /// established mapping configuration.
        /// </summary>
        /// <param name="configuration">Existing configuration.</param>
        public BasicSnapshotBehaviorProvider(Dictionary<Type, ISnapshotBehavior> configuration)
        {
           _configuration = configuration ?? new Dictionary<Type, ISnapshotBehavior>();
        }

        #endregion

        #region ISnapshotBehaviorProvider methods

        /// <summary>
        /// Gets the behavior according of a specific event type.
        /// Please note that this can be called multiple times from concurrent thread, so
        /// you should pay attention to thread safety in your own implementation.
        /// </summary>
        /// <param name="type">Event type.</param>
        /// <returns>Snapshot behavior.</returns>
        public ISnapshotBehavior? GetBehaviorForEventType(Type type)
        {
            if (_configuration.ContainsKey(type))
            {
                return _configuration[type];
            }
            return null;
        }

        #endregion

    }
}
