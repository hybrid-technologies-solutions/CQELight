using CQELight.Buses.RabbitMQ.Network;
using CQELight.Buses.RabbitMQ.Publisher;
using CQELight.Buses.RabbitMQ.Subscriber;
using CQELight.Tools.Extensions;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Common
{
    internal class RabbitCommonTools
    {
        public static void DeclareExchangesAndQueueForSubscriber(
            IModel channel,
            RabbitSubscriberConfiguration config)
        {
            if (config.UseDeadLetterQueue)
            {
                channel.ExchangeDeclare(Consts.CONST_DEAD_LETTER_EXCHANGE_NAME, "fanout", true, false, null);
                channel.QueueDeclare(Consts.CONST_DEAD_LETTER_QUEUE_NAME, true, false, false, null);
                channel.QueueBind(Consts.CONST_DEAD_LETTER_QUEUE_NAME, Consts.CONST_DEAD_LETTER_EXCHANGE_NAME, "", null);
            }

            DeclareExchanges(channel, config.NetworkInfos.ServiceExchangeDescriptions.Concat(config.NetworkInfos.DistantExchangeDescriptions));
            
            foreach (var queueDescription in config.NetworkInfos.ServiceQueueDescriptions)
            {
                if (config.UseDeadLetterQueue && !queueDescription.AdditionnalProperties.ContainsKey(Consts.CONST_DEAD_LETTER_EXCHANGE_RABBIT_KEY))
                {
                    queueDescription.AdditionnalProperties.Add(Consts.CONST_DEAD_LETTER_EXCHANGE_RABBIT_KEY, Consts.CONST_DEAD_LETTER_EXCHANGE_NAME);
                }
                DeclareQueue(channel, queueDescription);
            }
        }

        public static void DeclareExchangesAndQueueForPublisher(
            IModel channel,
            RabbitPublisherConfiguration config)
        {
            DeclareExchanges(channel, config.NetworkInfos.ServiceExchangeDescriptions.Concat(config.NetworkInfos.DistantExchangeDescriptions));
            config.NetworkInfos.ServiceQueueDescriptions.DoForEach(q => DeclareQueue(channel, q));
        }

        private static void DeclareQueue(IModel channel, RabbitQueueDescription queueDescription)
        {
            channel.QueueDeclare(queueDescription);
            foreach (var queueBinding in queueDescription.Bindings)
            {
                if (queueBinding.RoutingKeys?.Any() == true)
                {
                    foreach (var routingKey in queueBinding.RoutingKeys)
                    {
                        channel.QueueBind(
                            queue: queueDescription.QueueName,
                            exchange: queueBinding.ExchangeName,
                            routingKey: routingKey,
                            arguments: queueBinding.AdditionnalProperties);
                    }
                }
                else
                {
                    channel.QueueBind(
                        queue: queueDescription.QueueName,
                        exchange: queueBinding.ExchangeName,
                        routingKey: "",
                        arguments: queueBinding.AdditionnalProperties);
                }
            }
        }

        private static void DeclareExchanges(IModel channel, IEnumerable<RabbitExchangeDescription> exchanges)
        {
            foreach (var exchangeDescription in exchanges)
            {
                channel.ExchangeDeclare(exchangeDescription);
            }

        }
    }
}
