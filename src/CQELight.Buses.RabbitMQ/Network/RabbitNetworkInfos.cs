using System.Collections.Generic;

namespace CQELight.Buses.RabbitMQ.Network
{
    /// <summary>
    /// Differents possibility for RabbitMQ exchange strategy.
    /// </summary>
    public enum RabbitMQExchangeStrategy
    {
        /// <summary>
        /// Creates an exchange per service for events.
        /// Events are send as "fanout" (faster)
        /// Command are routed to destination by routing key.
        /// </summary>
        ExchangePerService,
        /// <summary>
        /// Creates a single exchange for all services for publication.
        /// Events are route by custom routing key strategy.
        /// Commands are routed to destination by routing key.
        /// </summary>
        SingleExchange,
        /// <summary>
        /// Totally custom network configuration.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Defintion of networks infos about RabbitMQ.
    /// </summary>
    public class RabbitNetworkInfos
    {
        #region Public static member

        /// <summary>
        /// Empty network configuration.
        /// </summary>
        public static RabbitNetworkInfos Empty
            => new RabbitNetworkInfos();

        #endregion

        #region Properties

        /// <summary>
        /// Collection of service own exchange descriptions.
        /// </summary>
        public List<RabbitExchangeDescription> ServiceExchangeDescriptions { get; set; } = new List<RabbitExchangeDescription>();

        /// <summary>
        /// Collection of other service in the system exchanges.
        /// </summary>
        public List<RabbitExchangeDescription> DistantExchangeDescriptions { get; set; } = new List<RabbitExchangeDescription>();

        /// <summary>
        /// Collection of service own queue descriptions.
        /// </summary>
        public List<RabbitQueueDescription> ServiceQueueDescriptions { get; set; } = new List<RabbitQueueDescription>();

        #endregion

        #region Ctor

        private RabbitNetworkInfos() { }

        #endregion

        #region Public static methods

        /// <summary>
        /// Retrieve a pre-configured network infos for the defined strategy.
        /// </summary>
        /// <param name="serviceName">Service name.</param>
        /// <param name="strategy">Strategy to apply.</param>
        /// <returns>Preconfigure network infos.
        /// Custom will return an empty one.
        /// SingleExchange will return one distant exchange and one queue with one binding, to configure for routing keys.
        /// ExchangePerService will return one local exchange and one queue with no binding, to configure with other system exchanges.</returns>
        public static RabbitNetworkInfos GetConfigurationFor(string serviceName, RabbitMQExchangeStrategy strategy)
        {
            switch (strategy)
            {
                case RabbitMQExchangeStrategy.Custom:
                    return new RabbitNetworkInfos();
                case RabbitMQExchangeStrategy.SingleExchange:
                    var globalExch = new RabbitExchangeDescription(Consts.CONST_CQE_EXCHANGE_NAME)
                    {
                        ExchangeContentType = ExchangeContentType.Both
                    };
                    return new RabbitNetworkInfos
                    {
                        ServiceExchangeDescriptions = new List<RabbitExchangeDescription>
                        {
                           globalExch
                        },
                        DistantExchangeDescriptions = new List<RabbitExchangeDescription>
                        {
                            globalExch
                        },
                        ServiceQueueDescriptions = new List<RabbitQueueDescription>
                        {
                            new RabbitQueueDescription (serviceName + "_queue")
                            {
                                Bindings = new List<RabbitQueueBindingDescription>
                                {
                                    new RabbitQueueBindingDescription(Consts.CONST_CQE_EXCHANGE_NAME)
                                }
                            }
                        }
                    };
                case RabbitMQExchangeStrategy.ExchangePerService:
                default:
                    return new RabbitNetworkInfos
                    {
                        ServiceExchangeDescriptions = new List<RabbitExchangeDescription>
                        {
                            new RabbitExchangeDescription(serviceName + "_exchange")
                        },
                        ServiceQueueDescriptions = new List<RabbitQueueDescription>
                        {
                            new RabbitQueueDescription(serviceName + "_queue")
                        }
                    };
            }
        }

        #endregion
    }
}
