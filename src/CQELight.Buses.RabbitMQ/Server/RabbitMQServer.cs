using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.InMemory.Events;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Server
{
    /// <summary>
    /// Server instance to listen to RabbitMQ and apply callback.
    /// </summary>
    [Obsolete("Use CQELight.Buses.RabbitMQ.Subscriber.RabbitMQSubscriber instead")]
    public class RabbitMQServer : DisposableObject
    {
        #region Members

        private readonly ILogger _logger;
        private readonly RabbitMQServerConfiguration _config;
        private List<EventingBasicConsumer> _consumers = new List<EventingBasicConsumer>();
        private IConnection? _connection;
        private IModel? _channel;
        private readonly InMemoryEventBus? _inMemoryEventBus;

        #endregion

        #region Ctor

        internal RabbitMQServer(
            ILoggerFactory loggerFactory,
            RabbitMQServerConfiguration? config = null,
            InMemoryEventBus? inMemoryEventBus = null)
        {
            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
                loggerFactory.AddProvider(new DebugLoggerProvider());
            }
            _logger = loggerFactory.CreateLogger<RabbitMQServer>();
            _config = config ?? RabbitMQServerConfiguration.Default;
            _inMemoryEventBus = inMemoryEventBus;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Start the RabbitMQ in-app server.
        /// </summary>
        public void Start()
        {
            _consumers = new List<EventingBasicConsumer>();
            _connection = GetConnection();
            _channel = GetChannel(_connection);

            var queueName = "cqelight.events." + _config.Emiter;
            var queueConfig = _config.QueueConfiguration;

            _channel.QueueDeclare(
                            queue: queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments:
                            queueConfig?.CreateAndUseDeadLetterQueue == true
                                ? new Dictionary<string, object> { ["x-dead-letter-exchange"] = $"{Consts.CONST_DEAD_LETTER_QUEUE_PREFIX}{queueName}" }
                                : null);
            if (queueConfig?.CreateAndUseDeadLetterQueue == true)
            {
                _channel.QueueDeclare(
                                queue: Consts.CONST_DEAD_LETTER_QUEUE_PREFIX + queueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false);
            }
            //_channel.QueueBind(queueName, Consts.CONST_CQE_EXCHANGE_NAME, Consts.CONST_ROUTING_KEY_ALL);
            _channel.QueueBind(queueName, Consts.CONST_CQE_EXCHANGE_NAME, _config.Emiter);

            var queueConsumer = new EventingBasicConsumer(_channel);
            queueConsumer.Received += OnEventReceived;
            _channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: queueConsumer);
            _consumers.Add(queueConsumer);
        }

        /// <summary>
        /// Stop the server and cleanup resources.
        /// </summary>
        public void Stop()
            => Dispose();

        #endregion

        #region Private methods

        private IConnection GetConnection()
        {
            return _config.ConnectionFactory.CreateConnection();
        }

        private IModel GetChannel(IConnection connection)
        {
            var channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: Consts.CONST_CQE_EXCHANGE_NAME,
                                    type: ExchangeType.Fanout,
                                    durable: true,
                                    autoDelete: false);
            return channel;
        }

        private async void OnEventReceived(object model, BasicDeliverEventArgs args)
        {
            if (args.Body?.Any() == true && model is EventingBasicConsumer consumer)
            {
                try
                {
                    var dataAsStr = Encoding.UTF8.GetString(args.Body);
                    var enveloppe = dataAsStr.FromJson<Enveloppe>();
                    if (enveloppe != null)
                    {
                        if (enveloppe.Emiter == _config.Emiter)
                        {
                            return;
                        }
                        if (!string.IsNullOrWhiteSpace(enveloppe.Data) && !string.IsNullOrWhiteSpace(enveloppe.AssemblyQualifiedDataType))
                        {
                            var objType = Type.GetType(enveloppe.AssemblyQualifiedDataType);
                            if (objType != null)
                            {
                                if (objType.GetInterfaces().Any(i => i.Name == nameof(IDomainEvent)))
                                {
                                    var evt = _config.QueueConfiguration.Serializer.DeserializeEvent(enveloppe.Data, objType);
                                    _config.QueueConfiguration.Callback?.Invoke(evt);
                                    if (_config.QueueConfiguration.DispatchInMemory && _inMemoryEventBus != null)
                                    {
                                        await _inMemoryEventBus.PublishEventAsync(evt).ConfigureAwait(false);
                                    }
                                    consumer.Model.BasicAck(args.DeliveryTag, false);
                                }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogErrorMultilines("RabbitMQServer : Error when treating event.", exc.ToString());
                    consumer.Model.BasicReject(args.DeliveryTag, false); //TODO make it configurable for retry or so
                }
            }
            else
            {
                _logger.LogWarning("RabbitMQServer : Empty message received or event fired by bad model !");
            }
        }

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            try
            {
                _channel?.Dispose();
                _connection?.Dispose();
                _consumers.DoForEach(c => c.Received -= OnEventReceived);
                _consumers.Clear();
            }
            catch
            {
                //Not throw exception on cleanup
            }
        }

        #endregion

    }
}
