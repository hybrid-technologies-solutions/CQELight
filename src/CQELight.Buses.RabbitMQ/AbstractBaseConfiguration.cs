using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace CQELight.Buses.RabbitMQ
{
    /// <summary>
    /// Base class for configuration
    /// </summary>
    public abstract class AbstractBaseConfiguration : BaseEventBusConfiguration
    {
        #region Properties

        /// <summary>
        /// Configured ConnectionFactory to access RabbitMQ instance(s).
        /// </summary>
        public ConnectionFactory ConnectionFactory { get; protected set; }
        /// <summary>
        /// Emiter application identity.
        /// </summary>
        public string Emiter { get; protected set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new configuration for connecting to a rabbitMQ server.
        /// </summary>
        /// <param name="emiter">Id/Name of application that is using the bus</param>
        /// <param name="connectionFactory">Configured connection factory used to access RabbitMQ instance(s).</param>
        /// <param name="eventsLifetime">Definition of events life time. If null, default
        /// is applied, which means that every event type has a lifetime of 1 day.</param>
        /// <param name="parallelDispatchEventTypes">Event types that allows parallel dispatch.</param>
        protected AbstractBaseConfiguration(string emiter, ConnectionFactory connectionFactory,
            IEnumerable<EventLifeTimeConfiguration>? eventsLifetime, IEnumerable<Type>? parallelDispatchEventTypes)
            : base(eventsLifetime, parallelDispatchEventTypes)
        {
            if (string.IsNullOrWhiteSpace(emiter))
            {
                throw new ArgumentException("AbstractBaseConfiguration.Ctor() : Emiter value should be provided.", nameof(emiter));
            }

            Emiter = emiter;
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        #endregion

    }
}
