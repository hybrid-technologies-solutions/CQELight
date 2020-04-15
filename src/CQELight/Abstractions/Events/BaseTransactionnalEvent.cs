using CQELight.Abstractions.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CQELight.Abstractions.Events
{
    /// <summary>
    /// Base class for helping to create transactionnal events.
    /// </summary>
    public abstract class BaseTransactionnalEvent : BaseDomainEvent, ITransactionnalEvent
    {
        #region ITransactionnalEvent

        /// <summary>
        /// Ordered collection of events of the transaction.
        /// </summary>
        public ImmutableQueue<IDomainEvent> Events { get; private set; } = ImmutableQueue<IDomainEvent>.Empty;

        #endregion

        #region Ctor

        /// <summary>
        /// Creation of a new transactionnal event, based on a collection of events.
        /// Note that events passed as parameter will be added in transaction in order that they appear.
        /// </summary>
        /// <param name="events">Collection of events.</param>
        protected BaseTransactionnalEvent(params IDomainEvent[] events)
        {
            EnqueueCollection(events);
        }

        /// <summary>
        /// Creation of a new transactionnal event, based on an already ordered collection of events.
        /// </summary>
        /// <param name="eventsQueue">Ordered collection of events.</param>
        protected BaseTransactionnalEvent(Queue<IDomainEvent> eventsQueue)
        {
            EnqueueCollection(eventsQueue);
        }

        #endregion

        #region Private methods

        private void EnqueueCollection(IEnumerable<IDomainEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }
            if (!events.Any() || !events.Skip(1).Any())
            {
                throw new ArgumentException("BaseTransactionnalEvent.Ctor() : Inconsitant number of events for a transaction " +
                   $"(it should be greater than 1). Actually : {events.Count()}");
            }
            foreach (var item in events)
            {
                Events = Events.Enqueue(item);
            }
        }

        #endregion
    }
}
