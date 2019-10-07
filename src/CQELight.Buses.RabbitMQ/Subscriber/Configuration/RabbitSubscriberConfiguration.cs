using CQELight.Events.Serializers;
using CQELight.Tools.Extensions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Subscriber.Configuration
{
    ///// <summary>
    ///// Strategy to consider for acknowledge messages.
    ///// </summary>
    //public enum AckStrategy
    //{
    //    /// <summary>
    //    /// Ack message when handling is successful.
    //    /// </summary>
    //    AckOnSucces,
    //    /// <summary>
    //    /// Ack message when receive it.
    //    /// </summary>
    //    AckOnReceive
    //}

    ///// <summary>
    ///// Representation of a subscriber configuration.
    ///// </summary>
    //public class RabbitSubscriberConfiguration
    //{
    //    #region Nested classes

    //    private class Exchange
    //    {
    //        public string Name { get; set; }
    //        public bool Internal { get; set; }
    //        public string Type { get; set; }
    //    }

    //    #endregion

    //    #region Public static methods

    //    /// <summary>
    //    /// Retrieve the current configuration for a given connection to Rabbit.
    //    /// </summary>
    //    /// <param name="connectionFactory">Connection factory to use for establishing default configuration.</param>
    //    /// <returns>Default configuration</returns>
    //    public static RabbitSubscriberConfiguration GetDefault(string emiter, ConnectionFactory connectionFactory)
    //    {
    //        var config = new RabbitSubscriberConfiguration();
    //        var handler = new HttpClientHandler
    //        {
    //            Credentials = new NetworkCredential(connectionFactory.UserName, connectionFactory.Password)
    //        };
    //        using (var client = new HttpClient(handler))
    //        {
    //            var baseUri = connectionFactory.Endpoint.Ssl?.Enabled == true ? "https://" : "http://"
    //                + connectionFactory.Endpoint.HostName + ":15672";
    //            var exchangesAsJson = client.GetStringAsync(new Uri(baseUri + "/api/exchanges/"
    //                + (connectionFactory.VirtualHost.In("", "/") ? "" : connectionFactory.VirtualHost))).GetAwaiter().GetResult();
    //            if (!string.IsNullOrWhiteSpace(exchangesAsJson))
    //            {
    //                var exchanges = JsonConvert.DeserializeObject<Exchange[]>(exchangesAsJson);
    //                config.ExchangeConfigurations =
    //                    exchanges
    //                    .Where(e => !e.Internal && !e.Name.StartsWith("amq") && !e.Name.StartsWith(Consts.CONST_DEAD_LETTER_QUEUE_PREFIX) && !string.IsNullOrWhiteSpace(e.Name))
    //                    .Select(e => new RabbitSubscriberExchangeConfiguration
    //                    {
    //                        ExchangeDetails = new Common.RabbitExchangeDetails
    //                        {
    //                            ExchangeName = e.Name,
    //                            ExchangeType = e.Type
    //                        },
    //                        RoutingKey = e.Type == ExchangeType.Topic ? emiter : "",
    //                        QueueConfiguration = new QueueConfiguration(new JsonDispatcherSerializer(), emiter + "_queue", true)
    //                    }).ToList();
    //            }
    //        }
    //        return config;
    //    }

    //    #endregion

    //    #region Properties

    //    /// <summary>
    //    /// Current configured exchange configuration.
    //    /// </summary>
    //    public IEnumerable<RabbitSubscriberExchangeConfiguration> ExchangeConfigurations { get; internal set; }

    //    /// <summary>
    //    /// Dead letter exchange configuration.
    //    /// </summary>
    //    public Dictionary<string, string> DeadLettreExchangeConfiguration { get; internal set; }
        
    //    /// <summary>
    //    /// Defines the AckStrategy to use.
    //    /// </summary>
    //    public AckStrategy AckStrategy { get; internal set; }

    //    #endregion
    //}
}
