using System;
using System.Collections.Generic;

namespace CQELight.Buses.RabbitMQ.Network
{
    /// <summary>
    /// Strategy to consider for acknowledge messages.
    /// </summary>
    public enum AckStrategy
    {
        /// <summary>
        /// Ack message when handling is successful.
        /// </summary>
        AckOnSucces,
        /// <summary>
        /// Ack message when receive it.
        /// </summary>
        AckOnReceive
    }

    /// <summary>
    /// Description of a queue used by RabbitMQ.
    /// </summary>
    public class RabbitQueueDescription
    {
        #region Properties

        /// <summary>
        /// Name of the queue.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Flag that indicates if object within the queue are considered durable.
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// Flag that indicates if the queue is exclusive, meaning only usable by initial declaring connection.
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// Flag that indicates if queue is autodelete, meaning queue is deleted when there's no subscriber anymore.
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Additionnal properties to set to the queue.
        /// </summary>
        public Dictionary<string, object> AdditionnalProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Collection of bindings for this specific queue.
        /// </summary>
        public List<RabbitQueueBindingDescription> Bindings { get; set; } = new List<RabbitQueueBindingDescription>();

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new queue description.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        public RabbitQueueDescription(
            string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("RabbitMQQueueDescription.ctor() : Queue name should be provided.", nameof(queueName));
            }

            QueueName = queueName;
        }

        #endregion
    }
}
