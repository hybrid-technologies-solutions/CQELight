using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Dispatcher.Configuration;
using CQELight.Dispatcher.Configuration.Internal;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CQELight.Dispatcher
{
    /// <summary>
    /// Dispatcher of events and commands.
    /// </summary>
    public class BaseDispatcher : IDispatcher
    {
        #region Static members

        private IScope? s_PrivateScope;
        private readonly ILogger s_Logger;
        private readonly DispatcherConfiguration s_Config;

        #endregion

        #region Static properties

        private IScope? s_Scope
        {
            get
            {
                if (s_PrivateScope == null && DIManager.IsInit)
                {
                    s_PrivateScope = DIManager.BeginScope();
                }
                return s_PrivateScope;
            }
        }

        #endregion

        #region ctor

        /// <summary>
        /// Create a new dispatcher instance.
        /// </summary>
        /// <param name="configuration">Dispatcher configuration.</param>
        /// <param name="scopeFactory">Factory of DI scope.</param>
        public BaseDispatcher(DispatcherConfiguration configuration, IScopeFactory? scopeFactory = null)
        {
            s_Config = configuration ?? DispatcherConfiguration.Current;
            if (scopeFactory != null)
            {
                s_PrivateScope = scopeFactory.CreateScope();
            }

            s_Logger =
                s_Scope?.Resolve<ILoggerFactory>()?.CreateLogger<BaseDispatcher>()
                ??
                new LoggerFactory().CreateLogger<BaseDispatcher>();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Publish a range of events.
        /// </summary>
        /// <param name="data">Collection of events with their associated context.</param>
        /// <param name="callerMemberName">Caller name.</param>
        public async Task PublishEventsRangeAsync(IEnumerable<(IDomainEvent Event, IEventContext? Context)> data,
            [CallerMemberName] string callerMemberName = "")
        {
            var tasks = new List<Task>();

            foreach (var item in
                data.GroupBy(m => m.Event.AggregateId)
                .Select(m => new { Data = m.ToList() }))
            {
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var (Event, Context) in item.Data)
                    {
                        var (eventType, eventConfiguration) = LogEventDataAndGetConfig(Event, Context, callerMemberName);
                        await PrivatePublishEventAsync(Event, Context, eventType, eventConfiguration).ConfigureAwait(false);
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Publish a range of events.
        /// </summary>
        /// <param name="events">Collection of events.</param>
        /// <param name="callerMemberName">Caller name.</param>
        public Task PublishEventsRangeAsync(IEnumerable<IDomainEvent> events, [CallerMemberName] string callerMemberName = "")
            => PublishEventsRangeAsync(events.Select(e => (e, null as IEventContext)));

        /// <summary>
        /// Publish asynchronously an event and its context within every bus that it's configured for.
        /// </summary>
        /// <param name="event">Event to dispatch.</param>
        /// <param name="context">Context to associate.</param>
        /// <param name="callerMemberName">Caller name.</param>
        public async Task PublishEventAsync(IDomainEvent @event, IEventContext? context = null, [CallerMemberName] string callerMemberName = "")
        {
            var (eventType, eventConfiguration) = LogEventDataAndGetConfig(@event, context, callerMemberName);
            await CoreDispatcher.PublishEventToSubscribers(@event, eventConfiguration?.IsSecurityCritical ?? false).ConfigureAwait(false);
            await PrivatePublishEventAsync(@event, context, eventType, eventConfiguration).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispatch asynchronously a command and its context within every bus that it's configured for.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="context">Context to associate.</param>
        /// <param name="callerMemberName">Calling method.</param>
        /// <returns>Awaiter of events.</returns>
        public async Task<Result> DispatchCommandAsync(ICommand command, ICommandContext? context = null, [CallerMemberName] string callerMemberName = "")
        {
            if (command == null)
            {
                return Result.Fail();
            }
            var commandType = command.GetType();
            s_Logger.LogInformation(() => $"Dispatcher : Beginning of sending command of type {commandType.FullName} from {callerMemberName}");
            s_Logger.LogInformation(() => $"Dispatcher : Type of context associated with command {commandType.FullName} : {(context == null ? "none" : context.GetType().FullName)}");

            s_Logger.LogDebug(() => $"Dispatcher : Command's data : {command.ToJson()}");

            s_Logger.LogThreadInfos();

            var commandConfiguration = s_Config.CommandDispatchersConfiguration.FirstOrDefault(e => new TypeEqualityComparer().Equals(e.CommandType, command.GetType()));
            await CoreDispatcher.PublishCommandToSubscribers(command, commandConfiguration?.IsSecurityCritical ?? false).ConfigureAwait(false);
            if (commandConfiguration != null)
            {
                var tasks = new List<Task<Result>>();
                foreach (var bus in commandConfiguration.BusesTypes)
                {
                    try
                    {
                        ICommandBus? busInstance = null;
                        if (s_Scope != null)
                        {
                            busInstance = s_Scope.Resolve(bus) as ICommandBus;
                        }
                        else
                        {
                            busInstance = bus.CreateInstance() as ICommandBus;
                        }
                        if (busInstance != null)
                        {
                            s_Logger.LogInformation(() => $"Dispatcher : Sending the command {commandType.FullName} on bus {bus.FullName}");
                            tasks.Add(busInstance.DispatchAsync(command, context));
                        }
                        else
                        {
                            s_Logger.LogWarning($"Dispatcher : Instance of command bus {bus.FullName} cannot be retrieved from scope.");
                        }
                    }
                    catch (Exception e)
                    {
                        s_Logger.LogErrorMultilines($"Dispatcher.DispatchCommandAsync() : Exception when sending command {commandType.FullName} on bus {bus.FullName}",
                            e.ToString());
                        tasks.Add(Task.FromResult(Result.Fail()));
                        commandConfiguration.ErrorHandler?.Invoke(e);
                    }
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                if (tasks.Count == 1)
                {
                    return tasks[0].Result;
                }
                return Result.Ok().Combine(tasks.Select(r => r.Result).ToArray());
            }
            else
            {
                s_Logger.LogWarning($"Dispatcher.DispatchCommandAsync() : No configuration has been defined for command {commandType.FullName}");
                return Result.Fail();
            }
        }

        #endregion

        #region Private methods
        private (Type eventType, EventDispatchConfiguration configuration) LogEventDataAndGetConfig(IDomainEvent @event, IEventContext? context, string callerMemberName)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }
            var eventType = @event.GetType();
            s_Logger.LogInformation(() => $"Dispatcher : Beginning of dispatch event of type {eventType.FullName} from {callerMemberName}");
            s_Logger.LogInformation(() => $"Dispatcher : Type of context associated to event {eventType.FullName} : {(context == null ? "none" : context.GetType().FullName)}");

            s_Logger.LogDebug(() => $"Dispatcher : Event data : {Environment.NewLine}{@event.ToJson()}");

            s_Logger.LogThreadInfos();

            var eventConfiguration = s_Config.EventDispatchersConfiguration.FirstOrDefault(e => new TypeEqualityComparer().Equals(e.EventType, @event.GetType()));
            return (eventType, eventConfiguration);
        }

        private async Task PrivatePublishEventAsync(IDomainEvent @event, IEventContext? context, Type eventType, EventDispatchConfiguration? eventConfiguration)
        {
            if (eventConfiguration != null)
            {
                foreach (var bus in eventConfiguration.BusesTypes)
                {
                    try
                    {
                        IDomainEventBus? busInstance = null;
                        if (s_Scope != null)
                        {
                            busInstance = s_Scope.Resolve(bus) as IDomainEventBus;
                        }
                        else
                        {
                            busInstance = bus.CreateInstance() as IDomainEventBus;
                        }
                        if (busInstance != null)
                        {
                            s_Logger.LogInformation(() => $"Dispatcher : Sending the event {eventType.FullName} on bus {bus.FullName}");
                            await busInstance.PublishEventAsync(@event, context).ConfigureAwait(false);
                        }
                        else
                        {
                            s_Logger.LogWarning($"Dispatcher : Instance of events bus {bus.FullName} cannot be retrieved from scope.");
                        }
                    }
                    catch (Exception e)
                    {
                        s_Logger.LogErrorMultilines($"Dispatcher.DispatchEventAsync() : Exception when sending event {eventType.FullName} on bus {bus.FullName}",
                            e.ToString());
                        eventConfiguration.ErrorHandler?.Invoke(e);
                    }
                }
                s_Logger.LogInformation(() => $"Dispatcher : End of sending event of type {eventType.FullName}");
            }
            else
            {
                s_Logger.LogWarning($"Dispatcher : No dispatching configuration has been found for event of type {eventType.FullName}");
            }
        }

        private bool EventTypeMatch(Type eventType, Type otherEventType)
            =>
                eventType == otherEventType // Same type
            ||
                (
                eventType.IsGenericType && otherEventType.IsGenericType // Generic event ...
             && eventType.GetGenericTypeDefinition() == otherEventType.GetGenericTypeDefinition() // ... with same generic definition ...
             && eventType.GetTypeInfo().GenericTypeParameters[0].GetTypeInfo() // ... and argument is ...
                    .ImplementedInterfaces.Any(i =>
                        i.IsAssignableFrom(otherEventType.GenericTypeArguments[0]) // ... an implemented interface!
                        ||
                        otherEventType.GenericTypeArguments[0]
                            .IsInHierarchySubClassOf(eventType.GetTypeInfo().GenericTypeParameters[0].BaseType))  // ... a class sub-type!
                )
            ;

        #endregion

    }
}
