using RabbitMQ.Client;
using System;

namespace CQELight.Buses.RabbitMQ.Server
{
    /// <summary>
    /// Configuration class to setup RabbitMQ server behavior.
    /// </summary>
    // TODO remove in v2.
    [Obsolete("Use RabbitConnectionInfos & RabbitNeworkInfos instead. Will be removed in v2")]
    public class RabbitMQServerConfiguration : AbstractBaseConfiguration
    {
        #region Static members

        /// <summary>
        /// Default configuration that targets localhost for messaging.
        /// </summary>
        public static RabbitMQServerConfiguration Default
            => new RabbitMQServerConfiguration("default",
                new ConnectionFactory
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
                }, QueueConfiguration.Empty);

        #endregion

        #region Properties

        /// <summary>
        /// Specific configuration of the queue.
        /// </summary>
        public QueueConfiguration QueueConfiguration { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new server configuration on a rabbitMQ server.
        /// </summary>
        /// <param name="emiter">Id/Name of application that is using the bus</param>
        /// <param name="connectionFactory">Configured connection factory</param>
        /// <param name="queueConfiguration">Queue configuration.</param>
        public RabbitMQServerConfiguration(string emiter,
                                           ConnectionFactory connectionFactory,
                                           QueueConfiguration queueConfiguration)
            : base(emiter, connectionFactory, null, null)
        {
            QueueConfiguration = queueConfiguration ?? throw new ArgumentNullException(nameof(queueConfiguration));
        }

        #endregion

    }
}
