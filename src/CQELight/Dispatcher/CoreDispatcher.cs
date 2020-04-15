using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Dispatcher.Configuration;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Force.DeepCloner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.Dispatcher
{
    /// <summary>
    /// The core dispatcher is the main dispatcher of the app. It is use for ease to work and manage all static references.
    /// </summary>
    public static class CoreDispatcher
    {
        #region Events

#pragma warning disable S3264 
        /// <summary>
        /// Custom callback when an event is published.
        /// </summary>
        public static event Func<IDomainEvent, Task>? OnEventDispatched;
        /// <summary>
        /// Custom callback when a range of events are published.
        /// </summary>
        public static event Func<IEnumerable<IDomainEvent>, Task>? OnEventsDispatched;
        /// <summary>
        /// Custom callback when a command is dispatched.
        /// </summary>
        public static event Func<ICommand, Task<Result>>? OnCommandDispatched;
        /// <summary>
        /// Custom callback when a message is dispatched.
        /// </summary>
        public static event Func<IMessage, Task>? OnMessageDispatched;
#pragma warning restore S3264 

        #endregion

        #region Static members

        private static IScope? s_PrivateScope;
        private static IDispatcher s_Instance;
        private static readonly ILogger s_Logger;
        private static readonly object s_threadSafety = new object();
        private static readonly SemaphoreSlim s_HandlerManagementLock = new SemaphoreSlim(1);
        private static readonly ConcurrentDictionary<Type, SemaphoreSlim> s_LockData = new ConcurrentDictionary<Type, SemaphoreSlim>();

        internal static ConcurrentBag<WeakReference<object>> s_EventHandlers = new ConcurrentBag<WeakReference<object>>();
        internal static ConcurrentBag<WeakReference<object>> s_CommandHandlers = new ConcurrentBag<WeakReference<object>>();
        internal static ConcurrentBag<WeakReference<object>> s_MessagesHandlers = new ConcurrentBag<WeakReference<object>>();
        internal static ConcurrentBag<WeakReference<object>> s_TransactionnalHandlers = new ConcurrentBag<WeakReference<object>>();

        #endregion

        #region Static properties

        private static IScope? s_Scope
        {
            get
            {
                if (s_PrivateScope == null && DIManager.IsInit)
                {
                    lock (s_threadSafety)
                    {
                        if (s_PrivateScope == null && DIManager.IsInit)
                        {
                            s_PrivateScope = DIManager.BeginScope();
                        }
                    }
                }
                return s_PrivateScope;
            }
        }

        #endregion

        #region Static accessor

        static CoreDispatcher()
        {
            s_Logger = (s_Scope?.Resolve<ILoggerFactory>() ?? new LoggerFactory(new[] { new DebugLoggerProvider() })).CreateLogger(typeof(CoreDispatcher));
            InitDispatcherInstance();
        }

        #endregion

        #region Public static mehtods

        /// <summary>
        /// Remove a handler instance from dispatcher.
        /// </summary>
        /// <param name="handler">Instance to delete.</param>
        public static void RemoveHandlerFromDispatcher(object handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            var (IsCommandHandler, IsEventHandler, IsMessageHandler, IsTransactionnalEventHandler) = GetHandlerTypeOf(handler);
            if (!IsEventHandler && !IsCommandHandler && !IsMessageHandler && !IsTransactionnalEventHandler)
            {
                return;
            }
            s_HandlerManagementLock.Wait();
            try
            {
                s_Logger.LogThreadInfos();

                void RemoveHandlerFrom(ref ConcurrentBag<WeakReference<object>> collection)
                {
                    var handlerReference = collection.FirstOrDefault(c =>
                    {
                        if (c.TryGetTarget(out object h))
                            return h == handler;
                        return false;
                    });
                    if (handlerReference != null)
                    {
                        s_Logger.LogInformation(() => $"Dispatcher : Remove an handler of type {handler.GetType()} from Dispatcher.");
                        collection = new ConcurrentBag<WeakReference<object>>(collection.Except(new[] { handlerReference }));
                    }
                };

                if (IsCommandHandler)
                {
                    RemoveHandlerFrom(ref s_CommandHandlers);
                }
                if (IsEventHandler)
                {
                    RemoveHandlerFrom(ref s_EventHandlers);
                }
                if (IsMessageHandler)
                {
                    RemoveHandlerFrom(ref s_MessagesHandlers);
                }
                if (IsTransactionnalEventHandler)
                {
                    RemoveHandlerFrom(ref s_TransactionnalHandlers);
                }
            }
            finally
            {
                s_HandlerManagementLock.Release();
            }
        }
        /// <summary>
        /// Add a staticly created instance of a specific handler directly in dispatcher. Will be used only in current process.
        /// </summary>
        /// <param name="handler">Handler instance.</param>
        public static void AddHandlerToDispatcher(object handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            var (IsCommandHandler, IsEventHandler, IsMessageHandler, IsTransactionnalEventHandler) = GetHandlerTypeOf(handler);
            if (!IsEventHandler && !IsCommandHandler && !IsMessageHandler && !IsTransactionnalEventHandler)
            {
                return;
            }
            s_HandlerManagementLock.Wait();
            try
            {
                void AddHandlerIfNotExistsIn(ConcurrentBag<WeakReference<object>> collection)
                {
                    if (!collection.Any(c =>
                    {
                        c.TryGetTarget(out object h);
                        return h == handler;
                    }))
                    {
                        s_Logger.LogInformation(() => $"Dispatcher : Adding an handler of type {handler.GetType()} in Dispatcher.");
                        collection.Add(new WeakReference<object>(handler));
                    }
                };

                if (IsCommandHandler)
                {
                    AddHandlerIfNotExistsIn(s_CommandHandlers);
                }
                if (IsEventHandler)
                {
                    AddHandlerIfNotExistsIn(s_EventHandlers);
                }
                if (IsMessageHandler)
                {
                    AddHandlerIfNotExistsIn(s_MessagesHandlers);
                }
                if (IsTransactionnalEventHandler)
                {
                    AddHandlerIfNotExistsIn(s_TransactionnalHandlers);
                }
                s_Logger.LogThreadInfos();
            }
            finally
            {
                s_HandlerManagementLock.Release();
            }
        }
        /// <summary>
        /// Publish a range of events.
        /// </summary>
        /// <param name="data">Collection of events with their associated context.</param>
        /// <param name="callerMemberName">Caller name.</param>
        public static async Task PublishEventsRangeAsync(IEnumerable<(IDomainEvent Event, IEventContext? Context)> data, [CallerMemberName] string callerMemberName = "")
        {
            if (OnEventsDispatched != null)
            {
                await OnEventsDispatched(data.Select(e => e.Event)).ConfigureAwait(false);
            }
            await s_Instance.PublishEventsRangeAsync(data, callerMemberName).ConfigureAwait(false);
        }

        /// <summary>
        /// Publish a range of events.
        /// </summary>
        /// <param name="events">Collection of events.</param>
        /// <param name="callerMemberName">Caller name.</param>
        public static async Task PublishEventsRangeAsync(IEnumerable<IDomainEvent> events, [CallerMemberName] string callerMemberName = "")
        {
            if (OnEventsDispatched != null)
            {
                await OnEventsDispatched(events).ConfigureAwait(false);
            }
            await PublishEventsRangeAsync(events.Select(e => (e, null as IEventContext))).ConfigureAwait(false);
        }

        /// <summary>
        /// Publish asynchronously an event and its context within every bus that it's configured for.
        /// </summary>
        /// <param name="event">Event to dispatch.</param>
        /// <param name="context">Context to associate.</param>
        /// <param name="callerMemberName">Caller name.</param>
        public static Task PublishEventAsync(IDomainEvent @event, IEventContext? context = null, [CallerMemberName] string callerMemberName = "")
            => s_Instance.PublishEventAsync(@event, context, callerMemberName);

        /// <summary>
        /// Dispatch asynchronously a command and its context within every bus that it's configured for.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="context">Context to associate.</param>
        /// <param name="callerMemberName">Calling method.</param>
        /// <returns>Awaiter of events.</returns>
        public static Task<Result> DispatchCommandAsync(ICommand command, ICommandContext? context = null, [CallerMemberName] string callerMemberName = "")
            => s_Instance.DispatchCommandAsync(command, context, callerMemberName);

        /// <summary>
        /// Dispatch to all in-memory alive handlers a specific message.
        /// </summary>
        /// <param name="message">Instance of message to dispatch..</param>
        /// <param name="waitForCompletion">Flag that indicates if handlers can be run in parallel or if they should wait one after another for completion.</param>
        public static async Task DispatchMessageAsync<T>(T message, bool waitForCompletion = true)
            where T : IMessage
        {
            var sem = s_LockData.GetOrAdd(typeof(T), _ => new SemaphoreSlim(1));
            await sem.WaitAsync().ConfigureAwait(false); // perform a lock per message type to allow parallel execution of different messages
            s_Logger.LogThreadInfos();
            s_Logger.LogInformation(() => $"Dispatcher : Beginning of dispatch a message of type {typeof(T).FullName}");

            s_Logger.LogDebug(() => $"Dispatcher : Message's data = {Environment.NewLine}{message.ToJson()}");

            try
            {
                if (OnMessageDispatched != null)
                {
                    foreach (Func<IMessage, Task> act in OnMessageDispatched.GetInvocationList().OfType<Func<IMessage, Task>>())
                    {
                        try
                        {
                            s_Logger.LogInformation(() => $"Dispatcher :Invoking action {act.Method.Name} on {act.Target.GetType().FullName} for " +
                                $"message of type {typeof(T).FullName}");
                        }
                        catch
                        {
                            //No stop if any error on log
                        }
                        try
                        {
                            await act(message).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            s_Logger.LogError($"Dispatcher.DispatchAppMessage() : Cannot invoke action {act.Method.Name} " +
                                $"on {act.Target.GetType().FullName} for message of type {typeof(T).FullName}. Exception data : {Environment.NewLine} {e}");
                        }
                    }
                }
                bool IsCompatible(Type vmType, Type eventType)
                {
                    return vmType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)
                           && i.GetGenericArguments().Contains(eventType));
                }

                var toDelete = new List<WeakReference<object>>();
                foreach (var vm in CoreDispatcher.s_MessagesHandlers)
                {
                    if (vm.TryGetTarget(out object handler))
                    {
                        var handlerType = handler.GetType();
                        var messageType = message.GetType();
                        if (IsCompatible(handlerType, message.GetType()))
                        {
                            try
                            {
                                s_Logger.LogInformation(() => $"Dispatcher : Invoking handle method on handler type {handlerType.FullName} " +
                                    $"for message of type {messageType.FullName}");
                                var methodInfo = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                                var method = Array.Find(methodInfo, m => m.Name == nameof(IMessageHandler<T>.HandleMessageAsync) && m.GetParameters()
                                        .Any(p => p.ParameterType == messageType));
                                if (method != null)
                                {
                                    var task = method.Invoke(handler, new object[] { message });
                                    if (waitForCompletion)
                                    {
                                        await (Task)(task);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                s_Logger.LogErrorMultilines("Dispatcher.DispatchAppMessage() : " +
                                    $"Cannot handle message on handler {handlerType.FullName} " +
                                    $"for message of type {messageType.FullName}", e.ToString());
                            }
                        }
                    }
                    else
                    {
                        s_Logger.LogInformation(() => $"Dispatcher : Removing handler of type {vm.GetType().FullName} because instance has been garbage collected.");
                        toDelete.Add(vm);
                    }
                }
                await s_HandlerManagementLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    s_MessagesHandlers = new ConcurrentBag<WeakReference<object>>(s_MessagesHandlers.Except(toDelete));
                }
                finally
                {
                    s_HandlerManagementLock.Release();
                }
            }
            finally
            {
                sem.Release();
            }
        }

        /// <summary>
        /// Try to retrieve a specific handler from CoreDispatcher event handlers collection.
        /// </summary>
        /// <param name="handlerType">Type of handler to try to retrieve.</param>
        /// <returns>Handler instance if found, null otherwise.</returns>
        public static object? TryGetEventHandlerByType(Type handlerType)
        {
            s_HandlerManagementLock.Wait();
            try
            {
                var reference = s_EventHandlers.FirstOrDefault(h => h.TryGetTarget(out object handlerInstance)
                   && new TypeEqualityComparer().Equals(handlerInstance.GetType(), handlerType));
                if (reference != null && reference.TryGetTarget(out object instance))
                {
                    return instance;
                }
                return null;
            }
            finally
            {
                s_HandlerManagementLock.Release();
            }
        }

        /// <summary>
        /// Try to get all event handlers staticly added for a specific event type.
        /// </summary>
        /// <param name="type">Type of event to search handlers for.</param>
        /// <returns>Collection of event handlers.</returns>
        public static IEnumerable<object> TryGetHandlersForEventType(Type type)
        {
            s_HandlerManagementLock.Wait();
            try
            {
                var results = new List<object>();
                var toDelete = new List<WeakReference<object>>();
                foreach (var handler in s_EventHandlers.Concat(s_TransactionnalHandlers))
                {
                    if (handler.TryGetTarget(out object instance))
                    {
                        if (instance.GetType().GetInterfaces()
                            .Any(i => i.IsGenericType
                                   && (i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>) || i.GetGenericTypeDefinition() == typeof(ITransactionnalEventHandler<>))
                                   && i.GenericTypeArguments[0] == type)
                            && !results.Any(r => r == instance))
                        {
                            results.Add(instance);
                        }
                    }
                    else
                    {
                        toDelete.Add(handler);
                    }
                }

                if (toDelete.Count > 0)
                {
                    var copy = s_EventHandlers.ToList();
                    s_EventHandlers = new ConcurrentBag<WeakReference<object>>();
                    foreach (var item in copy)
                    {
                        if (!toDelete.Any(d => d == item))
                        {
                            s_EventHandlers.Add(item);
                        }
                    }
                }
                return results;
            }
            finally
            {
                s_HandlerManagementLock.Release();
            }
        }

        /// <summary>
        /// Try to get event handler staticly added for a specific command type.
        /// </summary>
        /// <param name="type">Type of event to search handlers for.</param>
        /// <returns>Instance of event handler.</returns>
        public static IEnumerable<object> TryGetHandlersForCommandType(Type type)
        {
            s_HandlerManagementLock.Wait();
            try
            {
                List<object> result = new List<object>();
                var toDelete = new List<WeakReference<object>>();
                foreach (var handler in s_CommandHandlers)
                {
                    if (handler.TryGetTarget(out object instance))
                    {
                        if (instance.GetType().GetInterfaces()
                            .Any(i => i.IsGenericType
                                   && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                                   && i.GenericTypeArguments[0] == type))
                        {
                            result.Add(instance);
                        }
                    }
                    else
                    {
                        toDelete.Add(handler);
                    }
                }

                if (toDelete.Count > 0)
                {
                    var copy = s_CommandHandlers.ToList();
                    s_CommandHandlers = new ConcurrentBag<WeakReference<object>>();
                    foreach (var item in copy)
                    {
                        if (!toDelete.Any(d => d == item))
                        {
                            s_CommandHandlers.Add(item);
                        }
                    }
                }
                return result.AsEnumerable();
            }
            finally
            {
                s_HandlerManagementLock.Release();
            }
        }

        #endregion

        #region Internal static methods

        internal static async Task PublishEventToSubscribers(IDomainEvent @event, bool isEventSecurityCritical = false)
        {
            if (OnEventDispatched != null)
            {
                var eventType = @event.GetType();
                IDomainEvent eventInstance = @event;
                if (isEventSecurityCritical)
                {
                    eventInstance = @event.DeepClone();
                }
                foreach (Func<IDomainEvent, Task> act in OnEventDispatched.GetInvocationList().OfType<Func<IDomainEvent, Task>>())
                {
                    try
                    {
                        s_Logger.LogInformation(() => $"CoreDispatcher : Invoke of action {act.Method.Name} on" +
                            $" {(act.Target != null ? act.Target.GetType().FullName : act.Method.DeclaringType.FullName)} for event {eventType.FullName}");
                    }
                    catch
                    {
                        // No need to stop if any error in logging
                    }
                    try
                    {
                        await act(eventInstance).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        s_Logger.LogErrorMultilines($"CoreDispatcher.DispatchEventAsync() : Cannot call" +
                            $" action {act.Method.Name} one {(act.Target != null ? act.Target.GetType().FullName : act.Method.DeclaringType.FullName)}" +
                            $" for event {eventType.FullName}",
                            e.ToString());
                    }
                }
            }
        }

        internal static async Task PublishCommandToSubscribers(ICommand command, bool isCommandSecurityCritical = false)
        {
            if (OnCommandDispatched != null)
            {
                var commandType = command.GetType();
                ICommand commandInstance = command;
                if (isCommandSecurityCritical)
                {
                    commandInstance = command.DeepClone();
                }
                foreach (Func<ICommand, Task> act in OnCommandDispatched.GetInvocationList().OfType<Func<ICommand, Task>>())
                {
                    try
                    {
                        s_Logger.LogInformation(() => $"Dispatcher : Invoke action {act.Method.Name} on {(act.Target != null ? act.Target.GetType().FullName : act.Method.DeclaringType.FullName)} " +
                            $"for command {commandType.FullName}");
                    }
                    catch
                    {
                        //No need to stop if any error in logging
                    }
                    try
                    {
                        await act(commandInstance).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        s_Logger.LogErrorMultilines($"Dispatcher.DispatchCommandAsync() : " +
                            $"Cannot call action {act.Method.Name} on {(act.Target != null ? act.Target.GetType().FullName : act.Method.DeclaringType.FullName)} for command {commandType.FullName}",
                            e.ToString());
                    }
                }
            }
        }

        internal static void CleanRegistrations()
        {
            s_MessagesHandlers = new ConcurrentBag<WeakReference<object>>();
            s_CommandHandlers = new ConcurrentBag<WeakReference<object>>();
            s_EventHandlers = new ConcurrentBag<WeakReference<object>>();
        }

        #endregion

        #region Private static methods

        private static (bool IsCommandHandler, bool IsEventHandler, bool IsMessageHandler, bool IsTransactionnalEventHandler) GetHandlerTypeOf(object handler)
        {
            Type handlerType = handler.GetType();
            bool isEventHandler = handlerType.ImplementsRawGenericInterface(typeof(IDomainEventHandler<>));
            bool isCommandHandler = handlerType.ImplementsRawGenericInterface(typeof(ICommandHandler<>));
            bool isMessageHandler = handlerType.ImplementsRawGenericInterface(typeof(IMessageHandler<>));
            bool isTransactionnalEventHandler = handlerType.ImplementsRawGenericInterface(typeof(ITransactionnalEventHandler<>));

            return (isCommandHandler, isEventHandler, isMessageHandler, isTransactionnalEventHandler);
        }

        private static void InitDispatcherInstance()
        {
            try
            {
                if (s_Scope != null)
                {
                    s_Instance = s_Scope.Resolve<IDispatcher>();
                }
            }
            catch (Exception e)
            {
                s_Logger.LogError(e, "CoreDispatcher.InitDispatcherInstance() : Unable to retrieve dispatcher instance from IoC. Default dispatcher will be used.");
            }
            if (s_Instance == null)
            {
                s_Instance = new BaseDispatcher(DispatcherConfiguration.Current);
            }
        }

        #endregion

    }
}
