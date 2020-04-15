using CQELight.Buses.RabbitMQ.Network;
using RabbitMQ.Client.Events;

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
