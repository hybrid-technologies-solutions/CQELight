using CQELight.Buses.RabbitMQ.Network;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Subscriber.Internal
{
    internal class CustomRabbitConsumer : EventingBasicConsumer
    {
        #region Properties

        public RabbitQueueDescription QueueDescription { get; }

        #endregion

        #region Ctor

        public CustomRabbitConsumer(
            global::RabbitMQ.Client.IModel model,
            RabbitQueueDescription queueDescription) 
            : base(model)
        {
            QueueDescription = queueDescription;
        }

        #endregion
    }
}
