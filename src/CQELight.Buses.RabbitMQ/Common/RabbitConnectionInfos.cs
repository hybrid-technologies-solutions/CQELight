using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl;
using System;

namespace CQELight.Buses.RabbitMQ.Common
{
    /// <summary>
    /// Holding informations for connecting to RabbitMQ
    /// </summary>
    public class RabbitConnectionInfos
    {
        #region Static properties

        /// <summary>
        /// Gets RabbitMQ default connection informations (using localhost as hostname
        /// and guest/guest as username/password).
        /// </summary>
        public static RabbitConnectionInfos Default
            =>
            FromConnectionFactory(
                new ConnectionFactory
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
                },
                "CQELight_RabbitMQ_Default");

        #endregion

        #region Properties

        /// <summary>
        /// Configured ConnectionFactory to access RabbitMQ instance(s).
        /// </summary>
        public ConnectionFactory ConnectionFactory { get; protected set; }
        /// <summary>
        /// Name of the service on RabbitMQ.
        /// </summary>
        public string ServiceName { get; protected set; }

        #endregion

        #region Ctor

        private RabbitConnectionInfos(string serviceName, ConnectionFactory connectionFactory)
        {
            ServiceName = serviceName;
            ConnectionFactory = connectionFactory;
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Creates a new RabbitMQConnectionInfos from a RabbitMQ's ConnectionFactory.
        /// </summary>
        /// <param name="connectionFactory">Initialized connection factor.</param>
        /// <returns>New configured instance</returns>
        public static RabbitConnectionInfos FromConnectionFactory(
            ConnectionFactory connectionFactory, string serviceName)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }
            if (string.IsNullOrWhiteSpace(connectionFactory.HostName))
            {
                throw new ArgumentException("Provided connectionFactory seems to be not well parameterized (host is missing).");
            }
            return new RabbitConnectionInfos(serviceName, connectionFactory);
        }

        #endregion
    }
}
