using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Dispatcher;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.TestFramework
{
    /// <summary>
    /// Assertion upon synchronous actions.
    /// </summary>
    public class TestFrameworkActionAssertion
    {
        #region Members

        private readonly Action _action;
        private readonly Mock<IDispatcher>? _dispatcherMock;
        private static readonly SemaphoreSlim s_Semaphore = new SemaphoreSlim(1);

        #endregion

        #region Ctor

        internal TestFrameworkActionAssertion(Action action, Mock<IDispatcher>? dispatcherMock = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action), "TestFrameworkAssertion.ctor() : Action for assertion must be specified.");
            _dispatcherMock = dispatcherMock;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Check that no events are raised by executing the action.
        /// </summary>
        /// <param name="timeout">Timeout.</param>
        public void ThenNoEventShouldBeRaised(ulong timeout = 1000)
        {
            var events = new List<IDomainEvent>();
            if (_dispatcherMock == null)
            {
                s_Semaphore.Wait();
                try
                {
                    var lambda = new Func<IDomainEvent, Task>((e) =>
                    {
                        events.Add(e);
                        return Task.CompletedTask;
                    });
                    var lambdaMulti = new Func<IEnumerable<IDomainEvent>, Task>((evts) =>
                    {
                        events.AddRange(evts);
                        return Task.CompletedTask;
                    });
                    CoreDispatcher.OnEventDispatched += lambda;
                    CoreDispatcher.OnEventsDispatched += lambdaMulti;
                    try
                    {
                        Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                            .GetAwaiter().GetResult();
                    }
                    finally
                    {
                        CoreDispatcher.OnEventDispatched -= lambda;
                        CoreDispatcher.OnEventsDispatched -= lambdaMulti;
                    }
                }
                finally
                {
                    s_Semaphore.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.PublishEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<IEventContext>(),
                    It.IsAny<string>()))
                    .Callback((IDomainEvent evt, IEventContext _, string __) => events.Add(evt))
                    .Returns(Task.CompletedTask);
                _dispatcherMock.Setup(m => m.PublishEventsRangeAsync(It.IsAny<IEnumerable<(IDomainEvent, IEventContext)>>(),
                    It.IsAny<string>()))
                    .Callback((IEnumerable<(IDomainEvent, IEventContext)> data, string _) => events.AddRange(data.Select(e => e.Item1)))
                    .Returns(Task.CompletedTask);

                Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                    .GetAwaiter().GetResult();
            }
            if (events.Count > 0)
            {
                throw new TestFrameworkException($"No events where expected, however, events of type [" +
                    $"{string.Join($"{Environment.NewLine},", events.Select(e => e.GetType().FullName))} " +
                    $"] where dispatched.");
            }
        }

        /// <summary>
        /// Check that no commands are dispatched by executing the action.
        /// </summary>
        /// <param name="timeout">Tiemout.</param>
        public void ThenNoCommandAreDispatched(ulong timeout = 1000)
        {
            var commands = new List<ICommand>();
            if (_dispatcherMock == null)
            {
                s_Semaphore.Wait();
                try
                {
                    var lambda = new Func<ICommand, Task<Result>>(c => { commands.Add(c); return Task.FromResult(Result.Ok()); });
                    CoreDispatcher.OnCommandDispatched += lambda;
                    try
                    {
                        Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                            .GetAwaiter().GetResult();
                    }
                    finally
                    {
                        CoreDispatcher.OnCommandDispatched -= lambda;
                    }
                }
                finally
                {
                    s_Semaphore.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.DispatchCommandAsync(It.IsAny<ICommand>(), It.IsAny<ICommandContext>(),
                    It.IsAny<string>()))
                    .Callback((ICommand cmd, ICommandContext _, string __) => commands.Add(cmd))
                    .Returns(Task.FromResult(Result.Ok()));

                Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                    .GetAwaiter().GetResult();
            }
            if (commands.Count > 0)
            {
                throw new TestFrameworkException($"No commands where expected, however commands of type [" +
                    $"{string.Join($"{Environment.NewLine},", commands.Select(e => e.GetType().FullName))} " +
                    $"] where dispatched.");
            }
        }

        /// <summary>
        /// Check that a specific event type instance has been dispatched in the system.
        /// </summary>
        /// <typeparam name="T">Expected event type</typeparam>
        /// <param name="timeout">Timeout.</param>
        public T ThenEventShouldBeRaised<T>(ulong timeout = 1000) where T : class, IDomainEvent
        {
            T? @event = null;
            if (_dispatcherMock == null)
            {
                s_Semaphore.Wait();
                try
                {
                    var lambda = new Func<IDomainEvent, Task>(e =>
                    {
                        if (e is T)
                        {
                            @event = e as T;
                        }
                        return Task.CompletedTask;
                    });
                    var lambdaMulti = new Func<IEnumerable<IDomainEvent>, Task>((evts) =>
                    {
                        var evt = evts.FirstOrDefault(e => e is T);
                        if (evt != null)
                        {
                            @event = evt as T;
                        }
                        return Task.CompletedTask;
                    });
                    CoreDispatcher.OnEventDispatched += lambda;
                    CoreDispatcher.OnEventsDispatched += lambdaMulti;
                    try
                    {
                        Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        CoreDispatcher.OnEventDispatched -= lambda;
                        CoreDispatcher.OnEventsDispatched -= lambdaMulti;
                    }
                }
                finally
                {
                    s_Semaphore.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.PublishEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<IEventContext>(),
                       It.IsAny<string>()))
                       .Callback((IDomainEvent evt2, IEventContext _, string __) => @event = evt2 as T)
                       .Returns(Task.CompletedTask);

                Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                    .GetAwaiter().GetResult();
            }
            if (@event == null)
            {
                throw new TestFrameworkException($"L'évenement de type {typeof(T).Name} attendu par l'exécution d'une action n'a pas été dispatché.");
            }

            return @event;
        }

        /// <summary>
        /// Check that a bunch of events are raised in the system.
        /// </summary>
        /// <param name="timeout">Timeout.</param>
        public IEnumerable<IDomainEvent> ThenEventsShouldBeRaised(ulong timeout = 1000)
        {
            var events = new List<IDomainEvent>();
            if (_dispatcherMock == null)
            {
                s_Semaphore.Wait();
                try
                {
                    var lambda = new Func<IDomainEvent, Task>((e) =>
                    {
                        events.Add(e);
                        return Task.CompletedTask;
                    });
                    var lambdaMulti = new Func<IEnumerable<IDomainEvent>, Task>((evts) =>
                    {
                        events.AddRange(evts);
                        return Task.CompletedTask;
                    });
                    CoreDispatcher.OnEventDispatched += lambda;
                    CoreDispatcher.OnEventsDispatched += lambdaMulti;
                    try
                    {
                        Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        CoreDispatcher.OnEventDispatched -= lambda;
                        CoreDispatcher.OnEventsDispatched -= lambdaMulti;
                    }
                }
                finally
                {
                    s_Semaphore.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.PublishEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<IEventContext>(),
                    It.IsAny<string>()))
                    .Callback((IDomainEvent evt, IEventContext _, string __) => events.Add(evt))
                    .Returns(Task.CompletedTask);
                _dispatcherMock.Setup(m => m.PublishEventsRangeAsync(It.IsAny<IEnumerable<(IDomainEvent, IEventContext)>>(),
                    It.IsAny<string>()))
                    .Callback((IEnumerable<(IDomainEvent, IEventContext)> data, string _) => events.AddRange(data.Select(e => e.Item1)))
                    .Returns(Task.CompletedTask);

                Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                    .GetAwaiter().GetResult();
            }
            if (events.Count == 0)
            {
                throw new TestFrameworkException($"No events were dispatched, but some were excepted.");
            }

            return events;
        }

        /// <summary>
        /// Check that a bunch of commands are dispatch in the system.
        /// </summary>
        /// <param name="timeout">Timeout.</param>
        /// <returns>All dispatched commands, if any.</returns>
        public IEnumerable<ICommand> ThenCommandsAreDispatched(ulong timeout = 100, bool autoFakeHandlers = true)
        {
            var commands = new List<ICommand>();
            if (_dispatcherMock == null)
            {
                s_Semaphore.Wait();
                try
                {
                    var lambda = new Func<ICommand, Task<Result>>(c => { commands.Add(c); return Task.FromResult(Result.Ok()); });
                    CoreDispatcher.OnCommandDispatched += lambda;
                    try
                    {
                        Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        CoreDispatcher.OnCommandDispatched -= lambda;
                    }
                }
                finally
                {
                    s_Semaphore.Release();
                }
            }
            else
            {
                _ = _dispatcherMock.Setup(m => m.DispatchCommandAsync(It.IsAny<ICommand>(), It.IsAny<ICommandContext>(),
                      It.IsAny<string>()))
                    .Callback((ICommand cmd, ICommandContext __, string _) => commands.Add(cmd))
                    .Returns(Task.FromResult(Result.Ok()));

                Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                    .GetAwaiter().GetResult();
            }
            if (commands.Count == 0)
            {
                throw new TestFrameworkException("No command were dispatched, but some were expected.");
            }
            return commands;
        }

        /// <summary>
        /// Check that a specific command type instance is dispatched in the system.
        /// </summary>
        /// <typeparam name="T">Command type.</typeparam>
        /// <param name="timeout">Timeout.</param>
        /// <returns>Dispatched command.</returns>
        public T ThenCommandIsDispatched<T>(ulong timeout = 1000) where T : class, ICommand
        {
            T? command = null;
            if (_dispatcherMock == null)
            {
                s_Semaphore.Wait();
                try
                {
                    var lambda = new Func<ICommand, Task<Result>>(c =>
                    {
                        if (c is T)
                        {
                            command = c as T;
                        }
                        return Task.FromResult(Result.Ok());
                    });
                    CoreDispatcher.OnCommandDispatched += lambda;
                    try
                    {
                        Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        CoreDispatcher.OnCommandDispatched -= lambda;
                    }
                }
                finally
                {
                    s_Semaphore.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.DispatchCommandAsync(It.IsAny<ICommand>(), It.IsAny<ICommandContext>(),
                    It.IsAny<string>()))
                    .Callback((ICommand cmd, ICommandContext _, string __) => command = cmd as T)
                    .Returns(Task.FromResult(Result.Ok()));

                Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token)
                    .GetAwaiter().GetResult();
            }
            if (command == null)
            {
                throw new TestFrameworkException($"Command of type {typeof(T).Name} was expected, but wasn't dispatched.");
            }
            return command;
        }

        /// <summary>
        /// Check that no message are raison.
        /// </summary>
        /// <param name="waitTime">Milliseconds to wait before timeout.</param>
        public void ThenNoMessageShouldBeRaised(ulong waitTime = 1000)
        {
            s_Semaphore.Wait();
            var appMessages = new List<IMessage>();
            try
            {
                var lambda = new Func<IMessage, Task>((e) =>
                {
                    appMessages.Add(e);
                    return Task.CompletedTask;
                });
                CoreDispatcher.OnMessageDispatched += lambda;
                try
                {
                    Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)waitTime).Token).GetAwaiter().GetResult();
                }
                finally
                {
                    CoreDispatcher.OnMessageDispatched -= lambda;
                }
            }
            finally
            {
                s_Semaphore.Release();
            }
            if (appMessages.Count > 0)
            {
                throw new TestFrameworkException($"No messages were expected, however message(s) of type [" +
                    $"{string.Join($"{Environment.NewLine},", appMessages.Select(e => e.GetType().FullName))} " +
                    $"] was/were raised.");
            }
        }

        /// <summary>
        /// Check that a specific IMessage type has been raised within the system.
        /// </summary>
        /// <typeparam name="T">Type of expected IMessage.</typeparam>
        /// <param name="waitTime">Timeout.</param>
        public T ThenMessageShouldBeRaised<T>(ulong waitTime = 1000) where T : class, IMessage
        {
            s_Semaphore.Wait();
            T? appMessage = null;
            try
            {
                var lambda = new Func<IMessage, Task>(e =>
                {
                    if (e is T)
                    {
                        appMessage = e as T;
                    }
                    return Task.CompletedTask;
                });
                CoreDispatcher.OnMessageDispatched += lambda;
                try
                {
                    Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)waitTime).Token).GetAwaiter().GetResult();
                }
                finally
                {
                    CoreDispatcher.OnMessageDispatched -= lambda;
                }
            }
            finally
            {
                s_Semaphore.Release();
            }

            if (appMessage == null)
            {
                throw new TestFrameworkException($"Messages of type {typeof(T).Name} not raised before timeout.");
            }

            return appMessage;
        }

        /// <summary>
        /// Check that the CoreDispatcher has received some messages.
        /// </summary>
        /// <param name="timeout">Timeout.</param>
        public IEnumerable<IMessage> ThenMessagesShouldBeRaised(ulong timeout = 1000)
        {
            s_Semaphore.Wait();
            var appMessages = new List<IMessage>();
            try
            {
                var lambda = new Func<IMessage, Task>((e) =>
                {
                    appMessages.Add(e);
                    return Task.CompletedTask;
                });
                CoreDispatcher.OnMessageDispatched += lambda;
                try
                {
                    Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).GetAwaiter().GetResult();
                }
                finally
                {
                    CoreDispatcher.OnMessageDispatched -= lambda;
                }
            }
            finally
            {
                s_Semaphore.Release();
            }
            if (appMessages.Count == 0)
            {
                throw new TestFrameworkException("No messages dispatched within the system.");
            }

            return appMessages;
        }

        #endregion

    }
}
