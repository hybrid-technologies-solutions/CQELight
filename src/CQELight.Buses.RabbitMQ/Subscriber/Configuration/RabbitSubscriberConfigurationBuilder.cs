using CQELight.Buses.RabbitMQ.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Subscriber.Configuration
{
    ///// <summary>
    ///// Builder of subscriber configruation.
    ///// </summary>
    //public class RabbitSubscriberConfigurationBuilder
    //{
    //    #region Properties

    //    internal List<RabbitSubscriberExchangeConfiguration> ExchangesConfiguration { get; }
    //        = new List<RabbitSubscriberExchangeConfiguration>();

    //    private string _allQueueDeadLetterExchange;
    //    private readonly Dictionary<string, string> _deadLetterConfiguration = new Dictionary<string, string>();

    //    #endregion

    //    #region Public method

    //    /// <summary>
    //    /// Defines the name of the dead letter exchange to use for all configured queues.
    //    /// </summary>
    //    /// <param name="exchangeName">Name of the dead letter exchange to use.</param>
    //    /// <returns>Current instance.</returns>
    //    public RabbitSubscriberConfigurationBuilder UseDeadLetterExchangeForAllQueues(
    //        string exchangeName)
    //    {
    //        if (string.IsNullOrWhiteSpace(exchangeName))
    //        {
    //            throw new ArgumentException("UseDeadLetterExchangeForAllQueues : Exchange name must be provided", nameof(exchangeName));
    //        }

    //        _allQueueDeadLetterExchange = exchangeName;
    //        return this;
    //    }

    //    /// <summary>
    //    /// Defines the name of the dead letter exchange for a specific queue.
    //    /// </summary>
    //    /// <param name="exchangeName">Name of the dead letter exchange to use.</param>
    //    /// <param name="queueName">Name of the queue to bind to dead letter exchange.</param>
    //    /// <returns>Current instance.</returns>
    //    public RabbitSubscriberConfigurationBuilder UseDeadLettreExchangeForQueue(
    //        string exchangeName,
    //        string queueName)
    //    {
    //        if (string.IsNullOrWhiteSpace(exchangeName))
    //        {
    //            throw new ArgumentException("UseDeadLettreExchangeForQueue : Exchange name must be provided", nameof(exchangeName));
    //        }
    //        if (string.IsNullOrWhiteSpace(queueName))
    //        {
    //            throw new ArgumentException("UseDeadLettreExchangeForQueue : Queue name must be provided", nameof(exchangeName));
    //        }
    //        if (!_deadLetterConfiguration.ContainsKey(queueName))
    //        {
    //            _deadLetterConfiguration.Add(queueName, exchangeName);
    //        }
    //        else
    //        {
    //            throw new InvalidOperationException($"UseDeadLettreExchangeForQueue : Queue {queueName} has already been configured with " +
    //                $"dead letter exchange {exchangeName}");
    //        }
    //        return this;
    //    }

    //    /// <summary>
    //    /// Specify that system is listening from a specific exchange.
    //    /// </summary>
    //    /// <param name="queueName">Name of related queue.</param>
    //    /// <param name="details">Additionnal details about exchange</param>
    //    /// <returns>Current instance.</returns>
    //    public RabbitSubscriberConfigurationBuilder ListenFromExchange(
    //        RabbitExchangeDetails details,
    //        QueueConfiguration queueConfiguration = null,
    //        string routingKey = null)
    //    {
    //        if (details is null)
    //        {
    //            throw new ArgumentNullException(nameof(details));
    //        }

    //        ExchangesConfiguration.Add(new RabbitSubscriberExchangeConfiguration
    //        {
    //            QueueConfiguration = queueConfiguration,
    //            RoutingKey = routingKey ?? "",
    //            ExchangeDetails = details
    //        });
    //        return this;
    //    }

    //    /// <summary>
    //    /// Get the definitive configuration.
    //    /// </summary>
    //    /// <returns>Instance of the current configuration.</returns>
    //    public RabbitSubscriberConfiguration Build()
    //        => new RabbitSubscriberConfiguration
    //        {
    //            ExchangeConfigurations = ExchangesConfiguration
    //        };

    //    #endregion

    //}
}
