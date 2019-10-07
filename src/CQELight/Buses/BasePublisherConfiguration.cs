using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses
{
    /// <summary>
    /// Base class for any message publisher.
    /// </summary>
    public abstract class BasePublisherConfiguration
    {
        #region Properties

        /// <summary>
        /// Configuration for events lifetimes. 
        /// </summary>
        public IEnumerable<EventLifeTimeConfiguration> EventsLifetime { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Base ctor that contains a collection of lifetime configuraiton.
        /// </summary>
        /// <param name="eventsLifetime">Collection of lifetime configuration.</param>
        public BasePublisherConfiguration(
            IEnumerable<EventLifeTimeConfiguration> eventsLifetime)
        {
            EventsLifetime = eventsLifetime;
        }

        #endregion
    }
}
