using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Network
{
    /// <summary>
    /// RabbitMQ exchange description within a configured system.
    /// </summary>
    public sealed class RabbitExchangeDescription
    {
        #region Properties

        /// <summary>
        /// Exchange name.
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        /// Type of the exchange.
        /// </summary>
        public string ExchangeType { get; set; } = "fanout";

        /// <summary>
        /// If an exchange is set to durable, every message published on it will be kept after
        /// restarting rabbit instance.
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// If an exchange is set autodelete, when no
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Custom additionnal properties.
        /// </summary>
        public Dictionary<string, object> AdditionnalProperties { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new description of the exchange with the name.
        /// Other properties can be configured after creation.
        /// </summary>
        /// <param name="exchangeName">Name of the exchange currently describing.</param>
        public RabbitExchangeDescription(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
            {
                throw new ArgumentException("message", nameof(exchangeName));
            }

            ExchangeName = exchangeName;
        }

        #endregion
    }
}
