using CQELight.Abstractions.Events.Interfaces;
using CQELight.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.Buses
{
    /// <summary>
    /// Base abstract class for events bus configuration.
    /// </summary>
    public abstract class BaseEventBusConfiguration
    {
        #region Members

        internal List<Type> _parallelDispatchEventTypes
            = new List<Type>();

        #endregion

        #region Properties

        /// <summary>
        /// Configuration for events lifetimes.
        /// </summary>
        public IEnumerable<EventLifeTimeConfiguration> EventsLifetime { get; }
        /// <summary>
        /// Collection of event's types that allow parallel dispatch (meaning that when dispatching a collection of same events of this type, they're dispatch in parallel).
        /// </summary>
        public IEnumerable<Type> ParallelDispatchEventTypes
            => _parallelDispatchEventTypes.AsEnumerable();

        #endregion

        #region Ctor

        /// <summary>
        /// Base constructor for event bus configuration.
        /// </summary>
        /// <param name="eventsLifetime">Definition of events life time. If null, default
        /// is applied, which means that every event type has a lifetime of 1 day.</param>
        /// <param name="parallelDispatchEventTypes">Collection of type of events
        /// that allows parallelDispatch.</param>
        public BaseEventBusConfiguration(IEnumerable<EventLifeTimeConfiguration>? eventsLifetime, IEnumerable<Type>? parallelDispatchEventTypes)
        {
            if (eventsLifetime != null)
            {
                EventsLifetime = eventsLifetime;
            }
            else
            {
                EventsLifetime = ReflectionTools.GetAllTypes()
                    .Where(t => typeof(IDomainEvent).IsAssignableFrom(t))
                    .Select(t => new
                    EventLifeTimeConfiguration(t, TimeSpan.FromHours(1)));
            }

            _parallelDispatchEventTypes = (parallelDispatchEventTypes ?? Enumerable.Empty<Type>()).ToList();
        }

        #endregion

    }
}
