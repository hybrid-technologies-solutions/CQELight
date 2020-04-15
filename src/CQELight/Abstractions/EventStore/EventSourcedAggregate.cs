using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using System.Collections.Generic;

namespace CQELight.Abstractions.EventStore
{
    /// <summary>
    /// Base class for event sourced aggregates,
    /// that extends capabilities of standard DDD aggregate to
    /// allow management by an event store.
    /// It offers a protected member of type AggregateState to help
    /// you manage the current aggregate state, from a event-sourcing
    /// point of view.
    /// </summary>
    /// <typeparam name="T">Type of aggregate Id</typeparam>
    /// <typeparam name="TState">Typeof state to use.</typeparam>
    public abstract class EventSourcedAggregate<T, TState> : AggregateRoot<T>, IEventSourcedAggregate
        where TState : AggregateState
    {
        #region Properties members

        /// <summary>
        /// Current state of the aggregate.
        /// </summary>
        protected virtual TState? State { get; set; }

        #endregion

        #region IEventSourcedAggregate

        /// <summary>
        /// Rehydratation method that needs to be overriden in order to set back state
        /// to a good value, based on a collection of events.
        /// </summary>
        /// <param name="events">Events used to recreate the state.</param>
        public virtual void RehydrateState(IEnumerable<IDomainEvent> events) => State?.ApplyRange(events);

        #endregion

    }
}
