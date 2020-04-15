using CQELight.Abstractions.CQS.Interfaces;
using System;

namespace CQELight.Buses.InMemory.Commands
{
    /// <summary>
    /// A configuration builder for in memory command bus configuration.
    /// </summary>
    public class InMemoryCommandBusConfigurationBuilder
    {
        #region Members

        private readonly InMemoryCommandBusConfiguration _config
            = new InMemoryCommandBusConfiguration();

        #endregion

        #region Public methods

        /// <summary>
        /// Add a callback function when no handlers are found for a specific command.
        /// The callback will be fired with the command and its context, if any.
        /// </summary>
        /// <param name="onNoHandlersFound">Callback to fire.</param>
        /// <returns>Current builder.</returns>
        public InMemoryCommandBusConfigurationBuilder AddHandlerWhenHandlerIsNotFound(Action<ICommand, ICommandContext?> onNoHandlersFound)
        {
            _config.OnNoHandlerFounds = onNoHandlersFound;
            return this;
        }

        /// <summary>
        /// Defines a bus level to allow dispatching in memory only if a specific condition has been defined.
        /// </summary>
        /// <typeparam name="T">Type of concerned command</typeparam>
        /// <param name="condition">If clause</param>
        /// <returns>Current builder.</returns>
        public InMemoryCommandBusConfigurationBuilder DispatchOnlyIf<T>(Func<T, bool> condition)
            where T : class, ICommand
        {
            _config._ifClauses.Add(typeof(T), x =>
            {
                if (x is T xAsT)
                {
                    return condition(xAsT);
                }
                return false;
            });
            return this;
        }

        /// <summary>
        /// Allow multiple handlers for a specific command type.
        /// Altough this is not recommended, it could be usefull for some extra specific cases.
        /// </summary>
        /// <typeparam name="T">Typeof command that allows.</typeparam>
        /// <param name="waitForCompletionBeforeNext">Indicates if completion should be wait before going to next handler.</param>
        /// <returns>Current builder.</returns>
        public InMemoryCommandBusConfigurationBuilder AllowMultipleHandlersFor<T>(bool waitForCompletionBeforeNext = false)
            where T : class, ICommand
        {
            _config._multipleHandlersTypes.Add(new MultipleCommandHandlerConf(typeof(T))
            {
                ShouldWait = waitForCompletionBeforeNext
            });
            return this;
        }

        /// <summary>
        /// Get the config that has been builded.
        /// </summary>
        /// <returns>Builded configuration.</returns>
        public InMemoryCommandBusConfiguration Build()
            => _config;

        #endregion

    }
}
