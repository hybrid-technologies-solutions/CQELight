using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Dispatcher;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CQELight.Buses.InMemory.Commands
{
    /// <summary>
    /// Bus for dispatching commands.
    /// State is not handle in this bus, by definition, commands are stateless. If the system fail in any unexpected ways,
    /// the use wouldn't want its action to be replayed when system is up again.
    /// </summary>
    public class InMemoryCommandBus : DisposableObject, ICommandBus
    {
        #region Private members

        private static IEnumerable<Type> _handlers;
        private static IEnumerable<Type> Handlers
        {
            get
            {
                if (_handlers == null)
                {
                    InitHandlersCollection();
                }
                return _handlers;
            }
        }
        private InMemoryCommandBusConfiguration _config;
        private readonly IScope _scope;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        internal InMemoryCommandBus()
            : this(null, null)
        {

        }
        internal InMemoryCommandBus(InMemoryCommandBusConfiguration configuration = null,
                                    IScopeFactory scopeFactory = null)
        {
            if (scopeFactory != null)
            {
                _scope = scopeFactory.CreateScope();
            }

            _logger =
                _scope?.Resolve<ILoggerFactory>()?.CreateLogger<InMemoryCommandBus>()
                ??
                new LoggerFactory().CreateLogger<InMemoryCommandBus>();
            _config = configuration ?? InMemoryCommandBusConfiguration.Default;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Dispatch command and context to all handlers.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="context">Context associated to command</param>
        public async Task<Result> DispatchAsync(ICommand command, ICommandContext context = null)
        {
            var commandTypeName = command.GetType().FullName;
            _logger.LogInformation(() => $"InMemoryCommandBus : Beginning of dispatching a command of type {commandTypeName}");
            var commandTasks = new List<Task<Result>>();
            _config = _config ?? InMemoryCommandBusConfiguration.Default;
            var ifClause = _config.IfClauses.FirstOrDefault(i => i.Key == command.GetType()).Value;
            if (ifClause?.Invoke(command) == false)
            {
                _logger.LogInformation(() => $"InMemoryCommandBus : If condition for command type {commandTypeName} has returned false.");
                return Result.Ok();
            }
            var handlers = TryGetHandlerFromIoCContainer(command);
            if (!handlers.Any())
            {
                _logger.LogInformation(() => $"InMemoryCommandBus : Handler for command type {commandTypeName} not found in Ioc container, trying to get it from CoreDispatcher.");
                handlers = TryGetHandlersInstancesFromCoreDispatcher(command);
            }
            if (!handlers.Any())
            {
                _logger.LogInformation(() => $"InMemoryCommandBus : Handler for command type {commandTypeName} not found in CoreDispatcher, trying to instantiate if by reflection.");
                handlers = TryGetHandlersInstancesByReflection(command);
            }
            if (!handlers.Any())
            {
                _logger.LogWarning($"InMemoryCommandBus : No handlers for command type {commandTypeName} were found.");
                _config.OnNoHandlerFounds?.Invoke(command, context);
                return Result.Fail($"No handlers for command type {commandTypeName} were found.");
            }
            bool manyHandlersAndShouldWait = false;
            if (handlers.Skip(1).Any())
            {
                if (!_config.CommandAllowMultipleHandlers.Any(t => new TypeEqualityComparer().Equals(t.CommandType, command.GetType())))
                {
                    return Result.Fail($"the command of type {commandTypeName} have multiple handlers within the same process. " +
                        "If this is expected, you should update your configuration to allow multiple handlers for this specific command type, altough this is not recommended.");
                }
                else
                {
                    manyHandlersAndShouldWait
                        = _config.CommandAllowMultipleHandlers.FirstOrDefault(t => new TypeEqualityComparer().Equals(t.CommandType, command.GetType()))?.ShouldWait ?? false;
                }
            }
            var cmdHandlers = handlers.ToList();
            if (cmdHandlers.Count > 1)
            {
                cmdHandlers = cmdHandlers.OrderByDescending(h => h.GetType().GetCustomAttribute<HandlerPriorityAttribute>()?.Priority ?? 0).ToList();
            }
            foreach (var handler in cmdHandlers)
            {
                _logger.LogInformation(() => $"InMemoryCommandBus : Invocation of handler of type {handlers.GetType().FullName}");
                var handlerType = handler.GetType();
                var method = handlerType.GetMethods()
                        .First(m => m.Name == nameof(ICommandHandler<ICommand>.HandleAsync));
                try
                {
                    if (manyHandlersAndShouldWait)
                    {
                        var result = await ((Task<Result>)method.Invoke(handler, new object[] { command, context })).ConfigureAwait(false);
                        commandTasks.Add(Task.FromResult(result));
                    }
                    else
                    {
                        var t = (Task<Result>)method.Invoke(handler, new object[] { command, context });
                        commandTasks.Add(t);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogErrorMultilines($"InMemoryCommandBus.DispatchAsync() : Exception when trying to dispatch command {commandTypeName} to handler {handler.GetType().FullName}",
                        e.ToString());
                    if (handlerType.IsDefined(typeof(CriticalHandlerAttribute)))
                    {
                        Result r = Result.Fail($"Critical handler {handlerType.FullName} has failed, so next ones will not be called");
                        commandTasks.Add(Task.FromResult(r));
                        break;
                    }
                }
            }

            if (!manyHandlersAndShouldWait)
            {
                await Task.WhenAll(commandTasks).ConfigureAwait(false);
            }
            if (commandTasks.Count == 1)
            {
                return commandTasks[0].Result;
            }
            return Result.Ok().Combine(commandTasks.Select(t => t.Result).ToArray());
        }

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            try
            {
                _scope?.Dispose();
            }
            catch
            {
                //Disposing should not throw exceptions
            }
        }
        #endregion

        #region Private methods

        private IEnumerable<object> TryGetHandlersInstancesFromCoreDispatcher(ICommand command)
         => CoreDispatcher.TryGetHandlersForCommandType(command.GetType());

        private IEnumerable<object> TryGetHandlersInstancesByReflection(ICommand command)
             => Handlers.Where(h => h.GetInterfaces()
                    .Any(x => x.IsGenericType && x.GenericTypeArguments[0] == command.GetType()))
                    .Select(t => t.CreateInstance()).WhereNotNull();

        private IEnumerable<object> TryGetHandlerFromIoCContainer(ICommand command)
        {
            if (_scope != null)
            {
                var type = typeof(ICommandHandler<>).GetGenericTypeDefinition().MakeGenericType(command.GetType());
                try
                {
                    return _scope.ResolveAllInstancesOf(type).Cast<object>();
                }
                catch (Exception e)
                {
                    _logger.LogErrorMultilines($"InMemoryCommandBus.TryGetHandlerFromIoCContainer() : Cannot resolve handler of type {type.FullName} from IoC container.",
                        e.ToString());
                }
            }
            return Enumerable.Empty<object>();
        }

        #endregion

        #region Private static methods

        private static bool IsCommandHandler(Type x)
            => x.GetInterfaces().Any(y => y.IsGenericType
                                           && y.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                                           && x.IsClass;

        #endregion

        #region Internal static methods

        internal static void InitHandlersCollection(params string[] excludedDLLs)
        {
            _handlers = ReflectionTools.GetAllTypes(excludedDLLs)
                        .Where(IsCommandHandler).ToList();
        }

        #endregion

    }
}
