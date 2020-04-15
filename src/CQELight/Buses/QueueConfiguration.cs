using CQELight.Abstractions.Dispatcher;
using CQELight.Events.Serializers;
using System;

namespace CQELight.Buses
{
    /// <summary>
    /// Transport of queue information.
    /// </summary>
    public class QueueConfiguration
    {
        #region Static

        /// <summary>
        /// An empty queue configuration that not dispatch anywhere.
        /// </summary>
        public static QueueConfiguration Empty
            => new QueueConfiguration(new JsonDispatcherSerializer(), "CQELight__Empty", false, null, false);

        #endregion

        #region Properties

        /// <summary>
        /// Flag that indicates if a dispatch should be done in memory.
        /// If you want to use this option, you should enable InMemory buses in bootstrapper.
        /// </summary>
        public bool DispatchInMemory { get; private set; }
        /// <summary>
        /// Custom callback to invoke when retrieving data from queue.
        /// </summary>
        public Action<object>? Callback { get; private set; }
        /// <summary>
        /// Flag that indicates if a dead letter queue should be used.
        /// </summary>
        public bool CreateAndUseDeadLetterQueue { get; private set; }
        /// <summary>
        /// Serializer to use to get data from queue.
        /// </summary>
        public IDispatcherSerializer Serializer { get; private set; }
        /// <summary>
        /// Name of the queue
        /// </summary>
        public string QueueName { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new customisable queue listening configuration.
        /// </summary>
        /// <param name="serializer">Serializer to use to get data from the queue.</param>
        /// <param name="queueName">Name of the queue. If empty, a default value should be handled in extension.</param>
        /// <param name="dispatchInMemory">Flag that indicates if data should be distached in memory.</param>
        /// <param name="callback">Callback to invoke when receving data.</param>
        /// <param name="createAndUseDeadLetterQueue">Flag that indicates if create a specific dead letter queue, which means
        /// that all unhandled data are pushed back in.</param>
        public QueueConfiguration(IDispatcherSerializer serializer, string queueName = "", bool dispatchInMemory = true, Action<object>? callback = null,
            bool createAndUseDeadLetterQueue = false)
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            DispatchInMemory = dispatchInMemory;
            Callback = callback;
            CreateAndUseDeadLetterQueue = createAndUseDeadLetterQueue;
            QueueName = queueName;
        }

        #endregion

    }
}
