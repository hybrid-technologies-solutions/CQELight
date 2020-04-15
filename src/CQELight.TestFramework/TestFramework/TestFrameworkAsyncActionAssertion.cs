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
    /// Assertion upon an asynchronous action.
    /// </summary>
    public class TestFrameworkAsyncActionAssertion
    {
        #region Members

        private readonly Func<Task> _action;
        private readonly Mock<IDispatcher>? _dispatcherMock;

        private static readonly SemaphoreSlim s_lock = new SemaphoreSlim(1);

        #endregion

        #region Ctor

        internal TestFrameworkAsyncActionAssertion(Func<Task> action, Mock<IDispatcher>? dispatcherMock = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action), "TestFrameworkAsyncActionAssertion.ctor() : Action to invoke must be provided.");
            _dispatcherMock = dispatcherMock;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Assert that no events are raised by the action.
        /// </summary>
        /// <param name="timeout">Timeout.</param>
        public async Task ThenNoEventShouldBeRaised(ulong timeout = 1000)
        {
            var events = new List<IDomainEvent>();
            if (_dispatcherMock == null)
            {
                await s_lock.WaitAsync().ConfigureAwait(false);
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
                        await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        CoreDispatcher.OnEventDispatched -= lambda;
                        CoreDispatcher.OnEventsDispatched -= lambdaMulti;
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.PublishEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<IEventContext>(),
                    It.IsAny<string>()))
                    .Callback((IDomainEvent evt, IEventContext _, string __) => events.Add(evt))
                    .Returns(Task.CompletedTask);
                _dispatcherMock.Setup(m => m.PublishEventsRangeAsync(It.IsAny<IEnumerable<(IDomainEvent, IEventContext?)>>(),
                    It.IsAny<string>()))
                    .Callback((IEnumerable<(IDomainEvent, IEventContext)> data, string _) => events.AddRange(data.Select(e => e.Item1)))
                    .Returns(Task.CompletedTask);

                await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
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
        public async Task ThenNoCommandAreDispatched(ulong timeout = 1000)
        {
            var commands = new List<ICommand>();
            if (_dispatcherMock == null)
            {
                await s_lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var lambda = new Func<ICommand, Task<Result>>(c => { commands.Add(c); return Task.FromResult(Result.Ok()); });
                    CoreDispatcher.OnCommandDispatched += lambda;
                    try
                    {
                        await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        CoreDispatcher.OnCommandDispatched -= lambda;
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.DispatchCommandAsync(It.IsAny<ICommand>(), It.IsAny<ICommandContext>(),
                    It.IsAny<string>()))
                    .Callback((ICommand cmd, ICommandContext _, string __) => commands.Add(cmd))
                    .Returns(Task.FromResult(Result.Ok()));

                await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
            }

            if (commands.Count > 0)
            {
                throw new TestFrameworkException($"No commands where expected, however commands of type [" +
                    $"{string.Join($"{Environment.NewLine},", commands.Select(e => e.GetType().FullName))} " +
                    $"] where dispatched.");
            }
        }

        /// <summary>
        /// Check that no messages were raised on an async action.
        /// </summary>
        /// <param name="timeout">Timeout.</param>
        public async Task ThenNoMessageShouldBeRaised(ulong timeout = 1000)
        {
            await s_lock.WaitAsync().ConfigureAwait(false);
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
                    await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                }
                finally
                {
                    CoreDispatcher.OnMessageDispatched -= lambda;
                }
            }
            finally
            {
                s_lock.Release();
            }
            if (appMessages.Count > 0)
            {
                throw new TestFrameworkException("No messages were expected, however message(s) of type [" +
                    $"{string.Join($"{Environment.NewLine},", appMessages.Select(e => e.GetType().FullName))} " +
                    $"] was/were dispatched.");
            }
        }

        /// <summary>
        /// Check that a specific event type instance has been dispatched in the system.
        /// </summary>
        /// <typeparam name="T">Expected event type</typeparam>
        /// <param name="timeout">Timeout.</param>
        public async Task<T> ThenEventShouldBeRaised<T>(ulong timeout = 1000) where T : class, IDomainEvent
        {
            T? @event = null;
            if (_dispatcherMock == null)
            {
                await s_lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var lambda = new Func<IDomainEvent, Task>(e =>
                    {
                        if (e is T evt)
                        {
                            @event = evt;
                        }
                        return Task.CompletedTask;
                    });
                    var lambdaMulti = new Func<IEnumerable<IDomainEvent>, Task>((evts) =>
                    {
                        var evt = evts.FirstOrDefault(e => e is T);
                        if (evt != null)
                        {
                            @event = (T)evt;
                        }
                        return Task.CompletedTask;
                    });
                    CoreDispatcher.OnEventDispatched += lambda;
                    CoreDispatcher.OnEventsDispatched += lambdaMulti;
                    try
                    {
                        await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        CoreDispatcher.OnEventDispatched -= lambda;
                        CoreDispatcher.OnEventsDispatched -= lambdaMulti;
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.PublishEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<IEventContext>(),
                       It.IsAny<string>()))
                       .Callback((IDomainEvent evt2, IEventContext _, string __) => @event = evt2 as T)
                       .Returns(Task.CompletedTask);

                await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
            }
            if (@event == null)
            {
                throw new TestFrameworkException($"Event of type {typeof(T).Name} hasn't been dispatched before timeout.");
            }
            return @event;
        }

        /// <summary>
        /// Check that a bunch of events are raised in the system.
        /// </summary>
        /// <param name="timeout">Timeout.</param>
        public async Task<IEnumerable<IDomainEvent>> ThenEventsShouldBeRaised(ulong timeout = 1000)
        {
            var events = new List<IDomainEvent>();
            if (_dispatcherMock == null)
            {
                await s_lock.WaitAsync().ConfigureAwait(false);
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
                        await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        CoreDispatcher.OnEventDispatched -= lambda;
                        CoreDispatcher.OnEventsDispatched -= lambdaMulti;
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.PublishEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<IEventContext>(),
                    It.IsAny<string>()))
                    .Callback((IDomainEvent evt, IEventContext _, string __) => events.Add(evt))
                    .Returns(Task.CompletedTask);
                _dispatcherMock.Setup(m => m.PublishEventsRangeAsync(It.IsAny<IEnumerable<(IDomainEvent, IEventContext?)>>(),
                    It.IsAny<string>()))
                    .Callback((IEnumerable<(IDomainEvent, IEventContext)> data, string _) => events.AddRange(data.Select(e => e.Item1)))
                    .Returns(Task.CompletedTask);

                await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
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
        /// <param name="autoFakeHandlers"></param>
        /// <returns>All dispatched commands, if any.</returns>
        public async Task<IEnumerable<ICommand>> ThenCommandsAreDispatched(ulong timeout = 1000, bool autoFakeHandlers = true)
        {
            var commands = new List<ICommand>();
            if (_dispatcherMock == null)
            {
                await s_lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var lambda = new Func<ICommand, Task<Result>>(c => { commands.Add(c); return Task.FromResult(Result.Ok()); });
                    CoreDispatcher.OnCommandDispatched += lambda;
                    try
                    {
                        await Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        CoreDispatcher.OnCommandDispatched -= lambda;
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.DispatchCommandAsync(It.IsAny<ICommand>(), It.IsAny<ICommandContext>(),
                    It.IsAny<string>()))
                    .Callback((ICommand cmd, ICommandContext _, string __) => commands.Add(cmd))
                    .Returns(Task.FromResult(Result.Ok()));

                await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
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
        public async Task<T> ThenCommandIsDispatched<T>(ulong timeout = 1000) where T : class, ICommand
        {
            T? command = null;
            if (_dispatcherMock == null)
            {
                await s_lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var lambda = new Func<ICommand, Task<Result>>(c =>
                    {
                        if (c is T cmd)
                        {
                            command = cmd;
                        }
                        return Task.FromResult(Result.Ok());
                    });
                    CoreDispatcher.OnCommandDispatched += lambda;
                    try
                    {
                        await Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        CoreDispatcher.OnCommandDispatched -= lambda;
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }
            else
            {
                _dispatcherMock.Setup(m => m.DispatchCommandAsync(It.IsAny<ICommand>(), It.IsAny<ICommandContext>(),
                    It.IsAny<string>()))
                    .Callback((ICommand cmd, ICommandContext _, string __) => command = cmd as T)
                    .Returns(Task.FromResult(Result.Ok()));

                await Task.Run(_action.Invoke, new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
            }
            if (command == null)
            {
                throw new TestFrameworkException($"Command of type {typeof(T).Name} was expected, but wasn't dispatched.");
            }
            return command;
        }

        /// <summary>
        /// Check that a specific IMessage type has been raised within the system.
        /// </summary>
        /// <typeparam name="T">Type of expected IMessage.</typeparam>
        /// <param name="timeout">Timeout.</param>
        public async Task<T> ThenMessageShouldBeRaised<T>(ulong timeout = 1000) where T : class, IMessage
        {
            s_lock.Wait();
            T? appMessage = null;
            try
            {
                var lambda = new Func<IMessage, Task>(e =>
                {
                    if (e is T appMsg)
                    {
                        appMessage = appMsg;
                    }
                    return Task.CompletedTask;
                });
                CoreDispatcher.OnMessageDispatched += lambda;
                try
                {
                    await Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                }
                finally
                {
                    CoreDispatcher.OnMessageDispatched -= lambda;
                }
            }
            finally
            {
                s_lock.Release();
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
        public async Task<IEnumerable<IMessage>> ThenMessagesShouldBeRaised(ulong timeout = 1000)
        {
            s_lock.Wait();
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
                    await Task.Run(() => _action.Invoke(), new CancellationTokenSource((int)timeout).Token).ConfigureAwait(false);
                }
                finally
                {
                    CoreDispatcher.OnMessageDispatched -= lambda;
                }
            }
            finally
            {
                s_lock.Release();
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
