using System;
using System.Collections.Generic;

namespace CQELight.Buses.RabbitMQ.Common
{
    /// <summary>
    /// Exchange details for Rabbit.
    /// </summary>
    public class RabbitExchangeDetails
    {
        #region Properties

        /// <summary>
        /// Name of the exchange.
        /// </summary>
        public string ExchangeName { get; set; } = "";

        /// <summary>
        /// Type of the exchange.
        /// </summary>
        public string ExchangeType { get; set; } = "";

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
        public Dictionary<string, object>? AdditionnalProperties { get; set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of <see cref="RabbitExchangeDetails"/>.
        /// </summary>
        [Obsolete("Use fully qualified constructor instead. Will be remove in v2")]
        public RabbitExchangeDetails()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitExchangeDetails"/>
        /// with specified exchange informations.
        /// </summary>
        /// <param name="exchangeName">Name of the exchange</param>
        /// <param name="exchangeType">Type of the exchange</param>
        public RabbitExchangeDetails(
            string exchangeName,
            string exchangeType)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
            {
                throw new ArgumentException("RabbitExchangeDetails.ctor() : Exchange name must be provided", nameof(exchangeName));
            }

            if (string.IsNullOrWhiteSpace(exchangeType))
            {
                throw new ArgumentException("RabbitExchangeDetails.ctor() : Exchange type must be provided", nameof(exchangeType));
            }

            ExchangeName = exchangeName;
            ExchangeType = exchangeType;
        }

        #endregion
    }
}
