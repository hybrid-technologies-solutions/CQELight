using System;

namespace CQELight.Abstractions.Events.Interfaces
{
    /// <summary>
    /// A public class that represents a Domain Event
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique id of the event.
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// Time when event happens.
        /// </summary>
        DateTime EventTime { get; }
        /// <summary>
        /// Linked aggregate Id if any.
        /// </summary>
        object? AggregateId { get; }
        /// <summary>
        /// Type of aggregate linked to event.
        /// </summary>
        Type? AggregateType{ get; }
        /// <summary>
        /// Current sequence within aggregate's events stream.
        /// </summary>
        ulong Sequence { get; }
    }
}
