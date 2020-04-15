using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CQELight.Abstractions.Dispatcher.Interfaces
{
    /// <summary>
    /// Contract interface for dispatcher
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Publish a range of events.
        /// </summary>
        /// <param name="data">Collection of events with their associated context.</param>
        /// <param name="callerMemberName">Caller name.</param>
        Task PublishEventsRangeAsync(IEnumerable<(IDomainEvent Event, IEventContext? Context)> data, [CallerMemberName] string callerMemberName = "");
        /// <summary>
        /// Publish a range of events.
        /// </summary>
        /// <param name="events">Collection of events.</param>
        /// <param name="callerMemberName">Caller name.</param>
        Task PublishEventsRangeAsync(IEnumerable<IDomainEvent> events, [CallerMemberName] string callerMemberName = "");
        /// <summary>
        /// Publish asynchronously an event and its context within every bus that it's configured for.
        /// </summary>
        /// <param name="event">Event to dispatch.</param>
        /// <param name="context">Context to associate.</param>
        /// <param name="callerMemberName">Caller name.</param>
        Task PublishEventAsync(IDomainEvent @event, IEventContext? context = null, [CallerMemberName] string callerMemberName = "");
        /// <summary>
        /// Dispatch asynchronously a command and its context within every bus that it's configured for.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="context">Context to associate.</param>
        /// <param name="callerMemberName">Calling method.</param>
        /// <returns>Result of command execution.</returns>
        Task<Result> DispatchCommandAsync(ICommand command, ICommandContext? context = null, [CallerMemberName] string callerMemberName = "");
    }
}
