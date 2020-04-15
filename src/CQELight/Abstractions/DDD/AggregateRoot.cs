using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Dispatcher;
using CQELight.Tools.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.Abstractions.DDD
{
    /// <summary>
    /// Base definition for aggregate. An aggregate is an entity that manage a bunch of entities and value-objects by keeping they consistent.
    /// </summary>
    /// <typeparam name="T">Type of aggregate Id</typeparam>
    public abstract class AggregateRoot<T> : Entity<T>
    {
        #region Members

        private readonly Queue<IDomainEvent> _domainEvents = new Queue<IDomainEvent>();
        private readonly SemaphoreSlim _lockSecurity = new SemaphoreSlim(1);

        #endregion

        #region Ctor

        /// <summary>
        /// Parameterless ctor.
        /// </summary>
        protected AggregateRoot()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// List of domain events associated to the aggregate.
        /// Do not use this collection to dispatch them but use the public method "DispatchDomainEvents".
        /// Warning : events are not or
        /// </summary>
        public virtual IEnumerable<IDomainEvent> DomainEvents => _domainEvents;

        /// <summary>
        /// Publish all domain events holded by the aggregate.
        /// </summary>
        public Task PublishDomainEventsAsync()
            => PublishDomainEventsAsync(null);

        /// <summary>
        /// Publish all domain events holded by the aggregate with a specified dispatcher.
        /// </summary>
        /// <param name="dispatcher">Dispatcher used for publishing.</param>
        public async Task PublishDomainEventsAsync(IDispatcher? dispatcher)
        {
            await _lockSecurity.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_domainEvents.Count > 0)
                {
                    foreach (var evt in _domainEvents)
                    {
                        if (evt.AggregateId == null || evt.AggregateType == null)
                        {
                            if (evt is BaseDomainEvent baseEvt)
                            {
                                if (baseEvt.AggregateId == null)
                                {
                                    baseEvt.AggregateId = Id;
                                }
                                if (baseEvt.AggregateType == null)
                                {
                                    baseEvt.AggregateType = GetType();
                                }
                            }
                            else
                            {
                                var props = evt.GetType().GetAllProperties();
                                if (evt.AggregateId == null)
                                {
                                    var aggIdProp = props.FirstOrDefault(p => p.Name == nameof(IDomainEvent.AggregateId));
                                    aggIdProp?.SetMethod?.Invoke(evt, new object?[] { Id });
                                }
                                if (evt.AggregateType == null)
                                {
                                    var aggTypeProp = props.FirstOrDefault(p => p.Name == nameof(IDomainEvent.AggregateType));
                                    aggTypeProp?.SetMethod?.Invoke(evt, new object[] { GetType() });
                                }
                            }
                        }
                    }

                    if (dispatcher == null)
                    {
                        await CoreDispatcher.PublishEventsRangeAsync(_domainEvents).ConfigureAwait(false);
                    }
                    else
                    {
                        await dispatcher.PublishEventsRangeAsync(_domainEvents).ConfigureAwait(false);
                    }
                    _domainEvents.Clear();
                }
            }
            finally
            {
                _lockSecurity.Release();
            }
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Clear all domain events actualy stored.
        /// This cannot be undone.
        /// </summary>
        protected internal virtual void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// Add a domain event to the aggregate events collection.
        /// </summary>
        /// <param name="newEvent">Event to add.</param>
        protected virtual void AddDomainEvent(IDomainEvent newEvent)
        {
            _lockSecurity.Wait();
            try
            {
                if (newEvent != null)
                {
                    _domainEvents.Enqueue(newEvent);
                }
            }
            finally
            {
                _lockSecurity.Release();
            }
        }

        /// <summary>
        /// Add a range of domain events to the aggregate events collection.
        /// </summary>
        /// <param name="events">Collection of data to add</param>
        protected virtual void AddRangeDomainEvent(IEnumerable<IDomainEvent> events)
        {
            _lockSecurity.Wait();
            try
            {
                if (events?.Any() == true)
                {
                    foreach (var item in events)
                    {
                        _domainEvents.Enqueue(item);
                    }
                }
            }
            finally
            {
                _lockSecurity.Release();
            }
        }

        #endregion

    }
}
