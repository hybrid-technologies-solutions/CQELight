using CQELight.Abstractions.Dispatcher;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Events.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public RabbitPublisherConfiguration(
            IEnumerable<EventLifeTimeConfiguration> eventsLifetime = null)
            : base(eventsLifetime ?? Enumerable.Empty<EventLifeTimeConfiguration>())
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Informations for connection to RabbitMQ.
        /// </summary>
        public RabbitConnectionInfos ConnectionInfos { get; set; }

        /// <summary>
        /// Informations about RabbitMQ network.
        /// </summary>
        public RabbitNetworkInfos NetworkInfos { get; set; }

        /// <summary>
        /// Serializer instance.
        /// </summary>
        public IDispatcherSerializer Serializer { get; set; } = new JsonDispatcherSerializer();

        #endregion
    }
}
