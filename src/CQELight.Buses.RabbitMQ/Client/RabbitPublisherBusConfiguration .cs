using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace CQELight.Buses.RabbitMQ.Client
{
    /// <summary>
    /// Configuration data for RabbitMQ bus.
    /// </summary>
    [Obsolete("Use CQELight.Buses.RabbitMQ.Publisher.RabbitPublisherBusConfiguration instead")]
    public class RabbitPublisherBusConfiguration : AbstractBaseConfiguration
    {
        #region Static members

        /// <summary>
        /// Default configuration that targets localhost for messaging.
        /// </summary>
        public static RabbitPublisherBusConfiguration Default
            => new RabbitPublisherBusConfiguration("default", "localhost", "guest", "guest");

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new client configuration on a rabbitMQ server.
        /// </summary>
        /// <param name="emiter">Id/Name of application that is using the bus. Will be used for exchanges names.</param>
        /// <param name="host">The host to connect to.</param>
        /// <param name="userName">The username to use.</param>
        /// <param name="password">The password to use.</param>
        /// <param name="eventsLifetime">Collection of relation between event type and lifetime. You should fill this collection to
        /// indicates expiration date for some events.</param>
        /// <param name="parallelDispatchEventTypes">Event types that allows parallel dispatch.</param>
        public RabbitPublisherBusConfiguration(string emiter,
                                              string host,
                                              string userName,
                                              string password,
                                              IEnumerable<EventLifeTimeConfiguration>? eventsLifetime = null,
                                              IEnumerable<Type>? parallelDispatchEventTypes = null)
            : base(emiter, new ConnectionFactory { HostName = host, UserName = userName, Password = password },
                  eventsLifetime, parallelDispatchEventTypes)
        {
        }

        #endregion

    }
}
