using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using System;
using System.Collections.Generic;

namespace CQELight.Abstractions.EventStore.Interfaces
{
    /// <summary>
    /// Contract interface for reading eventstore.
    /// </summary>
    public interface IReadEventStore
    {
        /// <summary>
        /// Retrieve an asynchronous stream for all events that are attached
        /// to as specific aggregate type.
        /// </summary>
        /// <param name="aggregateType">Type of aggregate which we want all events from.</param>
        /// <returns>AsyncEnumerable of events.</returns>
        IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateType(Type aggregateType);
        /// <summary>
        /// Retrieve all events of a specific type from the eventstore
        /// </summary>
        /// <typeparam name="T">Type of event to retrieve.</typeparam>
        /// <returns>AsyncEnumerable of events.</returns>
        IAsyncEnumerable<T> GetAllEventsByEventType<T>()
            where T : class, IDomainEvent;
        /// <summary>
        /// Retrieve all events of a specific type from the eventstore.
        /// </summary>
        /// <param name="eventType">Type of event to retrieve.</param>
        /// <returns>AsyncEnumerable of events.</returns>
        IAsyncEnumerable<IDomainEvent> GetAllEventsByEventType(Type eventType);
        /// <summary>
        /// Retrieve an asynchronous stream for all events of a specific aggregate
        /// type with a specified aggregate id.
        /// </summary>
        /// <typeparam name="TAggregateType">Type of aggregate to retrieve all events.</typeparam>
        /// <typeparam name="TAggregateId">Type of aggregate id</typeparam>
        /// <param name="id">Value of the id of the aggregate.</param>
        /// <returns>AsyncEnumerable of events.</returns>
        IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId<TAggregateType, TAggregateId>(TAggregateId id)
            where TAggregateType : AggregateRoot<TAggregateId>
            where TAggregateId : notnull;
        /// <summary>
        /// Retrieve an asynchronous stream for all events of a specific aggregate
        /// type with a specified aggregate id.
        /// </summary>
        /// <param name="aggregateId">Value of the id of the aggregate.</param>
        /// <param name="aggregateType">Type of the aggregate.</param>
        /// <returns>AsyncEnumerable of events.</returns>
        IAsyncEnumerable<IDomainEvent> GetAllEventsByAggregateId(Type aggregateType, object aggregateId);
    }
}
