using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Abstractions.Saga.Interfaces;
using CQELight.Dispatcher;
using CQELight.IoC.Exceptions;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CQELight.Buses.InMemory.Events
{
    /// <summary>
    /// InMemory stateless bus to dispatch events.
    /// If program shutdowns unexpectedly, it means all events stored in it are lost and cannot be retrieved.
    /// However, this is a very fast bus for dispatch.
    /// </summary>
    public sealed class InMemoryEventBus : IDomainEventBus
    {
        #region Private static members

        private static IEnumerable<Type>? s_eventHandlers;
        private static readonly List<Type> s_ExcludedHandlersTypes = new List<Type>();

        #endregion

        #region Private members

        private readonly Dictionary<Type, MethodInfo> _handlers_HandleMethods;
        private readonly InMemoryEventBusConfiguration _config = InMemoryEventBusConfiguration.Default;
        private readonly IScope? _scope;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        internal InMemoryEventBus()
            :this(null, null)
        {
        }

        internal InMemoryEventBus(InMemoryEventBusConfiguration? configuration = null, IScopeFactory? scopeFactory = null)
        {
            InitHandlersCollection(new string[0]);
            if (scopeFactory != null)
            {
                _scope = scopeFactory.CreateScope();
            }
            _logger =
                _scope?.Resolve<ILoggerFactory>()?.CreateLogger<InMemoryEventBus>()
                ??
                new LoggerFactory().CreateLogger<InMemoryEventBus>();
            _handlers_HandleMethods = new Dictionary<Type, MethodInfo>();
            _config = configuration ?? InMemoryEventBusConfiguration.Default;
        }

        #endregion

        #region Internal static methods

        internal static void InitHandlersCollection(string[] excludedDLLs)
        {
            s_eventHandlers ??= ReflectionTools.GetAllTypes(excludedDLLs).Where(IsEventHandler).WhereNotNull().ToList();
        }

        #endregion

        #region Private static methods

        private static bool IsEventHandler(Type x)
            => x.GetInterfaces().Any(y => y.IsGenericType
                                       && (y.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>) || y.GetGenericTypeDefinition() == typeof(ITransactionnalEventHandler<>)));

        #endregion

        #region Private methods

        private IEnumerable<(Type HandlerType, HandlerPriority Priority)> GetHandlerForEventType(Type eventType)
        => s_eventHandlers.Where(handlerType => HandlerTypeCompatibleWithEvent(eventType, handlerType))
                .Select(handlerType =>
                {
                    HandlerPriority priority = handlerType.GetCustomAttribute<HandlerPriorityAttribute>()?.Priority ?? HandlerPriority.Normal;
                    return (handlerType, priority);
                });

        private bool HandlerTypeCompatibleWithEvent(Type eventType, Type handlerType)
         => handlerType?.GetInterfaces().Any(i =>
                            i.IsGenericType
                         && (i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>) || i.GetGenericTypeDefinition() == typeof(ITransactionnalEventHandler<>))
                         && i.GenericTypeArguments[0] == eventType) == true;

        private object? GetOrCreateHandler(Type handlerType, IEventContext? context)
        {
            object? result = null;
            try
            {
                var handlerFromCoreDispatcher = CoreDispatcher.TryGetEventHandlerByType(handlerType);
                if (handlerFromCoreDispatcher != null)
                {
                    result = handlerFromCoreDispatcher;
                }
                else
                {
                    if (typeof(ISaga).IsAssignableFrom(handlerType))
                    {
                        _logger.LogDebug(() => $"InMemoryEventBus : Getting a handler of type {handlerType.FullName} which is a saga.");
                        if (context != null && context.GetType() == handlerType)
                        {
                            if (context is ISaga saga && saga.Completed)
                            {
                                result = context;
                            }
                            else
                            {
                                _logger.LogWarning($"InMemoryEventBus : Trying to get a saga whereas it's already completed.");
                            }
                        }
                    }
                    else if (handlerType == context?.GetType())
                    {
                        _logger.LogDebug(() => $"InMemoryEventBus : Getting a handler of type {handlerType.FullName} which is current dispatch context.");
                        result = context;
                    }
                    else
                    {
                        if (s_ExcludedHandlersTypes.Contains(handlerType, new TypeEqualityComparer()))
                        {
                            return null;
                        }
                        if (context is IScope scope && !scope.IsDisposed)
                        {
                            _logger.LogDebug(() => $"InMemoryEventBus : Getting a handler of type {handlerType.FullName} from the context.");
                            result = scope.Resolve(handlerType);
                        }
                        else if (context is IScopeHolder scopeHolder && scopeHolder.Scope?.IsDisposed == false)
                        {
                            _logger.LogDebug(() => $"InMemoryEventBus : Getting a handler of type {handlerType.FullName} from the scope of the context.");
                            result = scopeHolder.Scope.Resolve(handlerType);
                        }
                        else
                        {
                            if (_scope != null)
                            {
                                _logger.LogDebug(() => $"InMemoryEventBus : Getting a handler of type {handlerType.FullName} from general scope.");
                                result = _scope.Resolve(handlerType);
                            }
                            else
                            {
                                _logger.LogDebug(() => $"InMemoryEventBus : Trying to get handler of type {handlerType.FullName} via reflexion.");
                                result = handlerType.CreateInstance();
                            }
                        }
                    }
                }
            }
            catch (IoCResolutionException iocEx)
            {
                if (!handlerType.NameExistsInHierarchy("BaseViewModel")) //ViewModels alway polute cause they need dynamic parameters at resolution
                {
                    _logger.LogErrorMultilines($"Cannot retrieve any handler of type {handlerType.FullName}" +
                        $" from IoC scope", iocEx.ToString());
                }
                s_ExcludedHandlersTypes.Add(handlerType);
            }
            catch (Exception ex)
            {
                if (!handlerType.NameExistsInHierarchy("BaseViewModel")) //ViewModels alway polute cause they need dynamic parameters at resolution
                {
                    _logger.LogErrorMultilines($"Cannot retrieve any handler of type {handlerType.FullName}", ex.ToString());
                }
            }

            return result;
        }

        private IEnumerable<(Type type, HandlerPriority priority)> GetOrderedHandlers(List<(Type HandlerType, HandlerPriority Priority)> handlers,
            IEnumerable<Type> dispatcherHandlerTypes)
            => handlers
                        .Where(t => !dispatcherHandlerTypes.Any(i => new TypeEqualityComparer().Equals(i, t.HandlerType)))
                        .Select(h => (h.HandlerType, h.Priority));

        private IEnumerable<EventHandlingInfos> GetMethods(IDomainEvent @event, IEventContext? context)
        {
            var methods = new Queue<EventHandlingInfos>();
            var handlerTypes = new List<Type>();
            var evtType = @event.GetType();

            var handlers = GetHandlerForEventType(evtType).ToList();
            var dispatcherHandlerInstances = CoreDispatcher.TryGetHandlersForEventType(evtType).ToList();

            bool hasAtLeastOneActiveHandler = handlers.Count > 0 || dispatcherHandlerInstances.Count > 0;

            if (hasAtLeastOneActiveHandler)
            {
                foreach (var (type, priority) in GetOrderedHandlers(handlers, dispatcherHandlerInstances.Select(i => i.GetType())))
                {
                    var handlerInstance = GetOrCreateHandler(type, context);
                    if (handlerInstance != null)
                    {
                        _logger.LogDebug(() => $"InMemoryEventBus : Got handler of type {type.Name} for event's type {evtType.Name}");
                        var handleMethod = type.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync), new[] { evtType, typeof(IEventContext) });
                        var handlingInfos = new EventHandlingInfos(handleMethod, handlerInstance, priority);
                        if (!methods.Any(m => m.Equals(handlingInfos)))
                        {
                            _logger.LogInformation(() => $"InMemoryEventBus : Add method HandleAsync of handler {type.FullName} for event's type {evtType.FullName}");
                            methods.Enqueue(handlingInfos);
                        }
                    }
                    else
                    {
                        _logger.LogInformation(() => $"InMemoryEventBus : No dynamic handlers of type {type.FullName} retrieved for event of type {evtType.FullName}");
                    }
                    handlerTypes.Add(type);
                }
                foreach (var handlerInstance in dispatcherHandlerInstances.WhereNotNull())
                {
                    var handlerType = handlerInstance.GetType();
                    if (!handlerTypes.Contains(handlerType))
                    {
                        var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync), new[] { evtType, typeof(IEventContext) });
                        var handlingInfos = new EventHandlingInfos(handleMethod, handlerInstance, HandlerPriority.Normal);
                        if (!methods.Any(m => m.Equals(handlingInfos)))
                        {
                            _logger.LogInformation(() => $"InMemoryEventBus : Add HandlerAsync on {handlerType.FullName} obtained from CoreDispatcher for event of type {evtType.FullName}");
                            methods.Enqueue(handlingInfos);
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation(() => $"InMemoryEventBus : No in memory handlers found for event of type {evtType.Name}.");
            }
            return methods.Distinct();
        }

        #endregion

        #region IDomainEventBus methods

        /// <summary>
        /// Register asynchronously an event to be processed by the bus.
        /// </summary>
        /// <param name="event">Event to register.</param>
        /// <param name="context">Context associated to the event..</param>
        public async Task<Result> PublishEventAsync(IDomainEvent @event, IEventContext? context = null)
        {
            if (@event != null)
            {
                var evtType = @event.GetType();
                _logger.LogInformation(() => $"InMemoryEventBus : Beginning of treating event's type {evtType.FullName}");

                var ifClause = _config.IfClauses.FirstOrDefault(i => new TypeEqualityComparer().Equals(i.Key, @event.GetType()));
                if (ifClause.Value?.Invoke(@event) == false)
                {
                    _logger.LogInformation(() => $"InMemoryEventBus : Dispatch clause for event of type {evtType.FullName} prevents dispatching in memory.");
                    return Result.Fail($"InMemoryEventBus : Dispatch clause for event of type {evtType.FullName} prevents dispatching in memory.");
                }

                var methods = GetMethods(@event, context);

                var handled = new List<EventHandlingInfos>();
                int currentTry = 0;
                bool globalBreak = false;
                bool allowParallelHandling = _config.ParallelHandling.Any(t => new TypeEqualityComparer().Equals(t, evtType));
                var taskResults = new List<Task<Result>>();
                do
                {
                    if (globalBreak)
                    {
                        break;
                    }
                    var tasks = new List<(EventHandlingInfos Infos, Task<Result> Task)>();
                    foreach (var infos in methods
                        .Where(t => !handled.Any(h => h.Equals(t)))
                        .OrderByDescending(m => m.HandlerPriority))
                    {
                        async Task<bool> ShouldBreakIfApplyingRetryStrategyAsync()
                        {
                            await Task.Delay((int)_config.WaitingTimeMilliseconds).ConfigureAwait(false);
                            if (infos.HandlerInstance.GetType().IsDefined(typeof(CriticalHandlerAttribute)))
                            {
                                if (_config.NbRetries == 0 || currentTry > _config.NbRetries)
                                {
                                    globalBreak = true;
                                    Result r = Result.Fail($"Critical handler {infos.HandlerInstance.GetType().FullName} has failed, next ones will not be called");
                                    tasks.Add((infos, Task.FromResult(r)));
                                }
                                return true;
                            }
                            return false;
                        }

                        try
                        {
                            if (allowParallelHandling)
                            {
                                tasks.Add((infos, (Task<Result>)infos.HandlerMethod.Invoke(infos.HandlerInstance, new object?[] { @event, context })));
                            }
                            else
                            {
                                var result =
                                    await ((Task<Result>)infos.HandlerMethod.Invoke(infos.HandlerInstance, new object?[] { @event, context })).ConfigureAwait(false);
                                if (!result.IsSuccess)
                                {
                                    currentTry++;
                                    if (await ShouldBreakIfApplyingRetryStrategyAsync().ConfigureAwait(false))
                                    {
                                        break;
                                    }
                                }
                                handled.Add(infos);
                                taskResults.Add(Task.FromResult(result));
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogErrorMultilines($"InMemoryEventBus.TreatEventsAsync : {currentTry}/{_config.NbRetries})" +
                                $"Failed to call HandleAsync on handler of type {infos.HandlerInstance.GetType().FullName} for event's type {evtType.FullName}", e.ToString());
                            currentTry++;
                            if (await ShouldBreakIfApplyingRetryStrategyAsync().ConfigureAwait(false))
                            {
                                break;
                            }
                        }
                    }
                    if (allowParallelHandling)
                    {
                        var task = Task.WhenAll(tasks.Select(t => t.Task));
                        try
                        {
                            await task.ConfigureAwait(false);
                            taskResults = tasks.Select(t => t.Task).ToList();
                            break;
                        }
                        catch (Exception e)
                        {
                            _logger.LogErrorMultilines("InMemoryEventBus.TreatEventsAsync : " +
                                $"{currentTry}/{_config.NbRetries}) Failed to call HandleAsync of an handler in parallel" +
                                $"for event's type {evtType.FullName}", e.ToString());
                            handled.AddRange(tasks.Where(t => t.Task.Status != TaskStatus.Faulted).Select(t => t.Infos));
                            await Task.Delay((int)_config.WaitingTimeMilliseconds).ConfigureAwait(false);
                            currentTry++;
                        }
                    }
                }
                while (handled.Count != methods.Count() && _config.NbRetries != 0 && currentTry <= _config.NbRetries);
                if (_config.NbRetries != 0 && currentTry > _config.NbRetries)
                {
                    _config.OnFailedDelivery?.Invoke(@event, context);
                    _logger.LogDebug(() => $"InMemoryEventBus : Cannot retrieve an handler in memory for event of type {evtType.Name}.");
                    return Result.Fail();
                }
                if (taskResults.Count == 0)
                {
                    return Result.Fail();
                }
                return Result.Ok().Combine(taskResults.Select(c => c.Result).ToArray());
            }
            return Result.Fail();
        }

        public async Task<Result> PublishEventRangeAsync(IEnumerable<IDomainEvent> events)
        {
            _logger.LogInformation(() => $"InMemoryEventBus : Beginning of treating bunch of events");
            var eventsGroup = events.GroupBy(d => new { d.AggregateId, d.AggregateType })
                .Select(g => new
                {
                    Type = g.Key,
                    Events = g.OrderBy(e => e.EventTime).ToList()
                }).ToList();

#pragma warning disable 
            Task.Run(() =>
            {
                _logger.LogDebug(() => $"InMemoryEventBus : Found {eventsGroup.Count} group(s) :");
                eventsGroup.ForEach(e => _logger.LogDebug(() => $"\t Event of type {e.Type} : {e.Events.Count} events"));
            });
#pragma warning restore

            var tasks = new List<Task<Result>>();
            foreach (var item in eventsGroup)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var innerTasks = new List<Task<Result>>();
                    foreach (var evtData in item.Events)
                    {
                        var evtType = evtData.GetType();
                        var allowParallelDispatch = _config.ParallelDispatch.Any(t => new TypeEqualityComparer().Equals(evtType, t));
                        if (allowParallelDispatch)
                        {
                            _logger.LogInformation(() => $"InMemoryEventBus : Beginning of parallel dispatching events of type {evtType.FullName}");
                            innerTasks.Add(PublishEventAsync(evtData));
                        }
                        else
                        {
                            _logger.LogInformation(() => $"InMemoryEventBus : Beginning of single op dispatching events of type {evtType.FullName}");
                            innerTasks.Add(Task.FromResult(await PublishEventAsync(evtData).ConfigureAwait(false)));
                        }
                    }
                    await Task.WhenAll(innerTasks).ConfigureAwait(false);
                    return Result.Ok().Combine(innerTasks.Select(t => t.Result).ToArray());
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            _logger.LogInformation(() => $"InMemoryEventBus : End of dispatching bunch of events");

            return Result.Ok().Combine(tasks.Select(r => r.Result).ToArray());
        }

        #endregion

    }
}
