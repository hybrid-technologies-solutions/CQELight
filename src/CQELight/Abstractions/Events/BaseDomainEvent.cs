using CQELight.Abstractions.Events.Interfaces;
using System;

namespace CQELight.Abstractions.Events
{
    /// <summary>
    /// Base class for domain events.
    /// </summary>
    public abstract class BaseDomainEvent : IDomainEvent
    {
        #region IDomainEvent properties

        /// <summary>
        /// Unique id of the event.
        /// </summary>
        public Guid Id { get; protected internal set; }

        /// <summary>
        /// Time when event happens.
        /// </summary>
        public DateTime EventTime { get; protected internal set; }

        /// <summary>
        /// Linked aggregate Id if any.
        /// </summary>
        public object? AggregateId { get; protected internal set; }

        /// <summary>
        /// Type of aggregate linked to event.
        /// </summary>
        public Type? AggregateType { get; protected internal set; }
        /// <summary>
        /// Current sequence within aggregate's events chain.
        /// </summary>
        public ulong Sequence { get; protected internal set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor.s
        /// </summary>
        protected BaseDomainEvent()
        {
            EventTime = DateTime.Now;
            Id = Guid.NewGuid();
        }

        #endregion

    }
}
