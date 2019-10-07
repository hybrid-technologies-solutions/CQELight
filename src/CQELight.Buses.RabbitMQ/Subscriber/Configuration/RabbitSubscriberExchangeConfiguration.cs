using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Dispatcher;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.RabbitMQ.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Subscriber.Configuration
{
    ///// <summary>
    ///// Representation of a rabbit subscriber configuration.
    ///// </summary>
    //public class RabbitSubscriberExchangeConfiguration
    //{
    //    #region Properties

    //    /// <summary>
    //    /// Corresponding queue name.
    //    /// </summary>
    //    public string QueueName => QueueConfiguration?.QueueName ?? ExchangeDetails.ExchangeName + "_queue";
        
    //    /// <summary>
    //    /// Routing key to match
    //    /// </summary>
    //    public string RoutingKey { get; internal set; }

    //    /// <summary>
    //    /// Additionnal details about rabbit exchange.
    //    /// </summary>
    //    public RabbitExchangeDetails ExchangeDetails { get; internal set; }

    //    /// <summary>
    //    /// Queue configuration.
    //    /// </summary>
    //    public QueueConfiguration QueueConfiguration { get; internal set; }

    //    #endregion

    //    #region Ctor

    //    internal RabbitSubscriberExchangeConfiguration()
    //    {

    //    }

    //    #endregion
    //}
}
