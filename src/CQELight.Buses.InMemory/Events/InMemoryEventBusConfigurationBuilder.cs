using CQELight.Abstractions.Events.Interfaces;
using System;

namespace CQELight.Buses.InMemory.Events
{
    /// <summary>
    /// A configuration builder for in-memory event bus.
    /// </summary>
    public class InMemoryEventBusConfigurationBuilder
    {
        #region Members

        private readonly InMemoryEventBusConfiguration _config = new InMemoryEventBusConfiguration();

        #endregion

        #region Public methods

        /// <summary>
        /// Set a retrying strategy for configuration, based on time between retries and
        /// a number of retries.
        /// </summary>
        /// <param name="timeoutBetweenTries">Time between each retries.</param>
        /// <param name="nbRetries">Number of total retries.</param>
        /// <returns>Current configuration builder.</returns>
        public InMemoryEventBusConfigurationBuilder SetRetryStrategy(ulong timeoutBetweenTries, byte nbRetries)
        {
            _config.WaitingTimeMilliseconds = timeoutBetweenTries;
            _config.NbRetries = nbRetries;
            return this;
        }

        /// <summary>
        /// Defines a callback to invoke when a dispatch exception is thrown.
        /// </summary>
        /// <param name="callback">Callback method</param>
        /// <returns>Current configuration builder.</returns>
        public InMemoryEventBusConfigurationBuilder DefineErrorCallback(Action<IDomainEvent, IEventContext?> callback)
        {
            _config.OnFailedDelivery = callback;
            return this;
        }

        /// <summary>
        /// Defines a bus level to allow dispatching in memory only if a specific condition has been defined.
        /// </summary>
        /// <typeparam name="T">Type of concerned event</typeparam>
        /// <param name="ifClause">If clause</param>
        /// <returns>Current configuration builder.</returns>
        public InMemoryEventBusConfigurationBuilder DispatchOnlyIf<T>(Func<T, bool> ifClause)
            where T : class, IDomainEvent
        {
            _config._ifClauses.Add(typeof(T), x =>
            {
                if (x is T xAsT)
                {
                    return ifClause(xAsT);
                }
                return false;
            });

            return this;
        }

        /// <summary>
        /// Remove thread safety between event handlers for a specific event, which mean
        /// that all event handlers will be launch simultaneously.
        /// </summary>
        /// <typeparam name="T">Type of event to allow parallel handling.</typeparam>
        /// <returns>Current configuration builder.</returns>
        public InMemoryEventBusConfigurationBuilder AllowParallelHandlingFor<T>()
            where T : class, IDomainEvent
        {
            _config._parallelHandling.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Remove thread safety between event handlers for some specific event types, which mean
        /// that all event handlers will be launch simultaneously.
        /// </summary>
        /// <param name="types">Collection of type that allow parallel handling.</param>
        /// <returns>Current configuration builder.</returns>
        public InMemoryEventBusConfigurationBuilder AllowParallelHandlingFor(params Type[] types)
        {
            if (types != null)
            {
                _config._parallelHandling.AddRange(types);
            }
            return this;
        }

        /// <summary>
        /// Remove waiter between each event of same dispatched in a collection of same event type.
        /// </summary>
        /// <typeparam name="T">Type of event to allow parallel dispatch.</typeparam>
        /// <returns>Current configuration builder.</returns>
        public InMemoryEventBusConfigurationBuilder AllowParallelDispatchFor<T>()
            where T : class, IDomainEvent
        {
            _config._parallelDispatch.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Remove waiter between each event of same dispatched in a collection of same event type.
        /// </summary>
        /// <returns>Current configuration builder.</returns>
        public InMemoryEventBusConfigurationBuilder AllowParallelDispatchFor(params Type[] types)
        {
            if (types != null)
            {
                _config._parallelDispatch.AddRange(types);
            }
            return this;
        }

        /// <summary>
        /// Retrieve the build configuration.
        /// </summary>
        /// <returns>Instance of configuration.</returns>
        public InMemoryEventBusConfiguration Build()
            => _config;

        #endregion

    }
}
