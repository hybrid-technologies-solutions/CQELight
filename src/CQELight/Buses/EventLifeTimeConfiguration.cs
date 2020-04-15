using System;

namespace CQELight.Buses
{
    /// <summary>
    /// Struct that holds event life time configuration for buses.
    /// </summary>
    public struct EventLifeTimeConfiguration
    {
        #region Properties

        /// <summary>
        /// Type of event.
        /// </summary>
        public Type EventType { get; }
        /// <summary>
        /// Lifetime value.
        /// </summary>
        public TimeSpan LifeTime { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new instance of eventLifeTimeConfiguration,
        /// by defining for a specific event type a life duration.
        /// </summary>
        /// <param name="eventType">Type of event.</param>
        /// <param name="lifeTime">Lifetime value</param>
        public EventLifeTimeConfiguration(Type eventType, TimeSpan lifeTime)
        {
            if (lifeTime.TotalSeconds <= 30)
            {
                throw new ArgumentException("EventLifeTimeConfiguration.ctor() : Lifetime should be at least 30 seconds.");
            }
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            LifeTime = lifeTime;
        }

        #endregion

    }
}
