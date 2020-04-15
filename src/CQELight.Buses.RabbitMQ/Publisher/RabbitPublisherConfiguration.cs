using CQELight.Abstractions.Dispatcher;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Common.Abstractions;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Events.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.Buses.RabbitMQ.Publisher
{
    /// <summary>
    /// Instance of Rabbit publisher configuration
    /// </summary>
    public class RabbitPublisherConfiguration : BasePublisherConfiguration
    {
        #region Ctor

        /// <summary>
        /// Creates a new Rabbit publishing configuration.
        /// </summary>
        /// <param name="eventsLifetime">Collection of event lifetime informations.</param>
        // TODO remove v2
        [Obsolete("Use fully qualified constructor instead. Will be remove in v2")]
        public RabbitPublisherConfiguration(
            IEnumerable<EventLifeTimeConfiguration>? eventsLifetime = null)
            : base(eventsLifetime ?? Enumerable.Empty<EventLifeTimeConfiguration>())
        {
        }

        /// <summary>
        /// Creates a new Rabbit publishing configuration.
        /// </summary>
        /// <param name="connectionInfos">Connection infos to reach Rabbit instance/cluster</param>
        /// <param name="networkInfos">Network topology informations</param>
        /// <param name="eventsLifetime">Collection of event lifetime informations.</param>
        public RabbitPublisherConfiguration(
            RabbitConnectionInfos connectionInfos,
            RabbitNetworkInfos networkInfos,
            IEnumerable<EventLifeTimeConfiguration>? eventsLifetime = null)
            : base(eventsLifetime ?? Enumerable.Empty<EventLifeTimeConfiguration>())
        {
            ConnectionInfos = connectionInfos ?? throw new System.ArgumentNullException(nameof(connectionInfos));
            NetworkInfos = networkInfos ?? throw new System.ArgumentNullException(nameof(networkInfos));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Informations for connection to RabbitMQ.
        /// </summary>
        public RabbitConnectionInfos ConnectionInfos { get; set; } = RabbitConnectionInfos.Default;

        /// <summary>
        /// Informations about RabbitMQ network.
        /// </summary>
        public RabbitNetworkInfos NetworkInfos { get; set; } = RabbitNetworkInfos.Empty;

        /// <summary>
        /// Serializer instance.
        /// </summary>
        public IDispatcherSerializer Serializer { get; set; } = new JsonDispatcherSerializer();

        /// <summary>
        /// Routing key factory to use when publishing data.
        /// </summary>
        public IRoutingKeyFactory RoutingKeyFactory { get; set; } = new RabbitDefaultRoutingKeyFactory();

        #endregion
    }
}
