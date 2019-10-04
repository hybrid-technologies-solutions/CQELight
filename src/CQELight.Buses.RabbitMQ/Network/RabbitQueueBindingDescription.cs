using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Network
{
    /// <summary>
    /// Description of a binding of a queue to a specific exchange.
    /// </summary>
    public class RabbitQueueBindingDescription
    {
        #region Properties

        /// <summary>
        /// Name of the exchange to bind to.
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        /// Collection of routing keys to filter.
        /// </summary>
        public List<string> RoutingKeys { get; set; } = new List<string>();

        /// <summary>
        /// Other custom properties.
        /// </summary>
        public Dictionary<string, object> AdditionnalProperties { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new queue binding description.
        /// </summary>
        /// <param name="exchangeName">Name of the exchange.</param>
        public RabbitQueueBindingDescription(
            string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
            {
                throw new ArgumentException("Exchange name must be provided.", nameof(exchangeName));
            }

            ExchangeName = exchangeName;
        }

        #endregion
    }
}
