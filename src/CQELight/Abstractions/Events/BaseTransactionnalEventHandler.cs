using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using System.Threading.Tasks;

namespace CQELight.Abstractions.Events
{
    /// <summary>
    /// Base class to help managing the handling of a transactionnal event handler.
    /// </summary>
    public abstract class BaseTransactionnalEventHandler<TEvent> : ITransactionnalEventHandler<TEvent>
        where TEvent : ITransactionnalEvent
    {
        #region Protected methods

        /// <summary>
        /// Entry point to help treating current event.
        /// </summary>
        /// <param name="evt">Current event.</param>
        protected abstract Task<Result> TreatEventAsync(IDomainEvent evt);

        /// <summary>
        /// Overridable method that takes place before any event is treated.
        /// </summary>
        protected virtual Task<Result> BeforeTreatEventsAsync()
        {
            return Task.FromResult(Result.Ok());
        }

        /// <summary>
        /// Overridable method that takes place after all event are treated.
        /// </summary>
        /// <returns></returns>
        protected virtual Task<Result> AfterTreatEventsAsync()
        {
            return Task.FromResult(Result.Ok());
        }

        #endregion

        #region ITransactionnalEventHandler

        /// <summary>
        /// Handle asynchronously a transactionnal event.
        /// </summary>
        /// <param name="transactionnalEvent">Transactionnal event instance.</param>
        /// <param name="context">Dispatching context.</param>
        public async Task<Result> HandleAsync(TEvent transactionnalEvent, IEventContext? context = null)
        {
            var queue = transactionnalEvent.Events;
            var result = await BeforeTreatEventsAsync().ConfigureAwait(false);
            IDomainEvent? evt = queue.Peek();
            while (evt != null)
            {
                result = result.Combine(await TreatEventAsync(evt).ConfigureAwait(false));
                queue = queue.Dequeue();
                if (!queue.IsEmpty)
                {
                    evt = queue.Peek();
                }
                else
                {
                    evt = null;
                }
            }
            return result.Combine(await AfterTreatEventsAsync().ConfigureAwait(false));
        }

        #endregion
    }
}