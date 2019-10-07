using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Network;
using System;
using System.Collections.Generic;
using System.Text;

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
        public RabbitNetworkInfos NetworkInfos{ get; set; }

        /// <summary>
        /// Flag that indicates if dead letter queue shoud be used.
        /// </summary>
        public bool UseDeadLetterQueue { get; set; }

        #endregion
    }
}
