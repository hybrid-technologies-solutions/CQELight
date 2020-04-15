using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Network;
using RabbitMQ.Client.Framing.Impl;
using System;

namespace CQELight.Buses.RabbitMQ.Subscriber
{
    /// <summary>
    /// Configuration for RabbitMQ subscriber.
    /// </summary>
    public class RabbitSubscriberConfiguration
    {
        #region Properites

        /// <summary>
        /// Informations for connection to RabbitMQ.
        /// </summary>
        public RabbitConnectionInfos ConnectionInfos { get; set; }

        /// <summary>
        /// Informations about the network configuration within RabbitMQ.
        /// </summary>
        public RabbitNetworkInfos NetworkInfos { get; set; }

        /// <summary>
        /// Flag that indicates if dead letter queue shoud be used.
        /// </summary>
        public bool UseDeadLetterQueue { get; set; }

        /// <summary>
        /// Flag that indicates if receveid ressource (event or command) should be dispatched on the in-memory buses.
        /// </summary>
        public bool DispatchInMemory { get; set; } = true;

        /// <summary>
        /// Custom callback when an event is received.
        /// </summary>
        public Action<IDomainEvent>? EventCustomCallback { get; set; }

        /// <summary>
        /// Custom callback when a command is received.
        /// </summary>
        public Action<ICommand>? CommandCustomCallback { get; set; }

        /// <summary>
        /// Strategy to consider for ack.
        /// </summary>
        public AckStrategy AckStrategy { get; set; } = AckStrategy.AckOnSucces;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of <see cref="RabbitSubscriberConfiguration"/>.
        /// </summary>
        [Obsolete("Use full qualified constructor instead. Will be removed in v2")]
        public RabbitSubscriberConfiguration()
        {
            ConnectionInfos = RabbitConnectionInfos.Default;
            NetworkInfos = RabbitNetworkInfos.Empty;
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitSubscriberConfiguration"/>.
        /// </summary>
        /// <param name="connectionInfos">Connection infos to reach Rabbit instance/cluster</param>
        /// <param name="networkInfos">Network topology informations</param>
        public RabbitSubscriberConfiguration(
            RabbitConnectionInfos connectionInfos,
            RabbitNetworkInfos networkInfos)
        {
            ConnectionInfos = connectionInfos ?? throw new ArgumentNullException(nameof(connectionInfos));
            NetworkInfos = networkInfos ?? throw new ArgumentNullException(nameof(networkInfos));
        }

        #endregion
    }
}
