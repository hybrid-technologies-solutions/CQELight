﻿using RabbitMQ.Client;

namespace CQELight.Buses.RabbitMQ.Extensions
{
    internal static class IModelExtensions
    {
        #region Extensions methods

        public static void CreateCQEExchange(this IModel channel)
        {
            channel.ExchangeDeclare(exchange: Consts.CONST_CQE_EXCHANGE_NAME,
                                        type: ExchangeType.Fanout,
                                        durable: true,
                                        autoDelete: false);
        }

        #endregion

    }
}
