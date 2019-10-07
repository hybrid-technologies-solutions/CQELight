using CQELight.Buses.RabbitMQ.Network;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Common
{
    internal static class RabbitExtensionsMethods
    {
        public static void QueueDeclare(this IModel channel, RabbitQueueDescription queueDescription)
            => channel.QueueDeclare(
                queue: queueDescription.QueueName,
                durable: queueDescription.Durable,
                exclusive: queueDescription.Exclusive,
                autoDelete: queueDescription.AutoDelete,
                arguments: queueDescription.AdditionnalProperties);

        public static void ExchangeDeclare(this IModel channel, RabbitExchangeDescription exchangeDescription)
            => channel.ExchangeDeclare(
                exchange: exchangeDescription.ExchangeName,
                type: exchangeDescription.ExchangeType,
                durable: exchangeDescription.Durable,
                autoDelete: exchangeDescription.AutoDelete,
                arguments: exchangeDescription.AdditionnalProperties);
    }
}
