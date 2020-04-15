using CQELight.Abstractions.Events.Interfaces;
using System.Collections.Generic;

namespace CQELight.Abstractions.EventStore.Interfaces
{
    /// <summary>
    /// Contract interface for event sourced aggregates.
    /// State management remains private and is aggregate responsability.
    /// </summary>
    public interface IEventSourcedAggregate
    {
        /// <summary>
        /// Rehydratation method that needs to be overriden in order to set back state
        /// to a good value, based on a collection of events.
        /// </summary>
        /// <param name="events">Events used to recreate the state.</param>
        void RehydrateState(IEnumerable<IDomainEvent> events);
    }
}
