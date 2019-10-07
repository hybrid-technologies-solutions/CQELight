using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Events.Serializers;
using System;
using System.Collections.Generic;
using System.Text;

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
        public string QueueName { get; }

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

        /// <summary>
        /// Flag that indicates if receveid ressource (event or command) should be dispatched on the in-memory buses.
        /// </summary>
        public bool DispatchInMemory { get; set; } = true;

        /// <summary>
        /// Custom callback when an event is received.
        /// </summary>
        public Action<IDomainEvent> EventCustomCallback { get; set; } = null;

        /// <summary>
        /// Custom callback when a command is received.
        /// </summary>
        public Action<ICommand> CommandCustomCallback { get; set; } = null;

        /// <summary>
        /// Event serializer.
        /// </summary>
        public IEventSerializer EventSerializer { get; set; } = new JsonDispatcherSerializer();

        /// <summary>
        /// Command serializer.
        /// </summary>
        public ICommandSerializer CommandSerializer { get; set; } = new JsonDispatcherSerializer();

        /// <summary>
        /// Strategy to consider for ack.
        /// </summary>
        public AckStrategy AckStrategy { get; set; } = AckStrategy.AckOnSucces;

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
