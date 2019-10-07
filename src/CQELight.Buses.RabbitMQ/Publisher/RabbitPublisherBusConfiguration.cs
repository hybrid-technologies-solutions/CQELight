//using CQELight.Abstractions.Events.Interfaces;
//using CQELight.Buses.RabbitMQ.Configuration.Publisher;
//using RabbitMQ.Client;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace CQELight.Buses.RabbitMQ.Publisher
//{
//    /// <summary>
//    /// Configuration data for RabbitMQ bus.
//    /// </summary>
//    public class RabbitPublisherBusConfiguration : AbstractBaseConfiguration
//    {
//        #region Static members

//        /// <summary>
//        /// Default configuration that targets localhost for messaging.
//        /// </summary>
//        public static RabbitPublisherBusConfiguration Default
//            => new RabbitPublisherBusConfiguration("default", new ConnectionFactory
//            {
//                HostName = "localhost",
//                UserName = "guest",
//                Password = "guest"
//            });

//        #endregion

//        #region Properties

//        internal RabbitPublisherConfiguration PublisherConfiguration { get; set; }

//        #endregion

//        #region Ctor
        
//        /// <summary>
//        /// Create a new client configuration on a rabbitMQ server.
//        /// </summary>
//        /// <param name="emiter">Id/Name of application that is using the bus. Will be used for exchanges names.</param>
//        /// <param name="connectionFactory">RabbitMQ Connection Factory for accessing Rabbit instance(s).</param>
//        /// <param name="configuration">Configuration for publishing.</param>
//        /// <param name="eventsLifetime">Collection of relation between event type and lifetime. You should fill this collection to 
//        /// indicates expiration date for some events.</param>
//        /// <param name="parallelDispatchEventTypes">Event types that allows parallel dispatch.</param>
//        public RabbitPublisherBusConfiguration(string emiter,
//                                              ConnectionFactory connectionFactory,
//                                              RabbitPublisherConfiguration configuration = null,
//                                              IEnumerable<EventLifeTimeConfiguration> eventsLifetime = null,
//                                              IEnumerable<Type> parallelDispatchEventTypes = null)
//            : base(emiter, connectionFactory, eventsLifetime, parallelDispatchEventTypes)
//        {
//            PublisherConfiguration = configuration ?? RabbitPublisherConfiguration.GetDefault(emiter);
//        }

//        #endregion

//    }
//}
