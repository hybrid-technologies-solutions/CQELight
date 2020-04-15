using CQELight.Abstractions.DDD;
using System.Threading.Tasks;

namespace CQELight.Abstractions.Events.Interfaces
{
    /// <summary>
    /// Contract interface for handler that need to handle a transactionnal event.
    /// </summary>
    /// <typeparam name="T">Type of transactionnal event to handle.</typeparam>
    public interface ITransactionnalEventHandler<T>
        where T : ITransactionnalEvent
    {
        /// <summary>
        /// Handle asynchronously a transactionnal event.
        /// </summary>
        /// <param name="transactionnalEvent">Transactionnal event instance.</param>
        /// <param name="context">Dispatching context.</param>
        /// <returns>Result of corresponding operation.</returns>
        Task<Result> HandleAsync(T transactionnalEvent, IEventContext? context = null);
    }
}
