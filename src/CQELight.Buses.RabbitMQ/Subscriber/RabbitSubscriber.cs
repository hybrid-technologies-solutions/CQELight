using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Events.Serializers;
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

namespace CQELight.Buses.RabbitMQ.Subscriber
{
    /// <summary>
    /// Subscriber instance that will do callbacks when pumping messages from
    /// rabbit queue.
    /// </summary>
    public class RabbitSubscriber : DisposableObject
    {
        #region Members

        private readonly ILogger _logger;
        private readonly RabbitSubscriberConfiguration _config;
        private List<EventingBasicConsumer> _consumers = new List<EventingBasicConsumer>();
        private IConnection? _connection;
        private IModel? _channel;
        private readonly IScopeFactory? scopeFactory;

        #endregion

        #region Ctor

        public RabbitSubscriber(
            ILoggerFactory loggerFactory,
            RabbitSubscriberConfiguration config,
            IScopeFactory? scopeFactory = null)
        {
            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
                loggerFactory.AddProvider(new DebugLoggerProvider());
            }
            _logger = loggerFactory.CreateLogger<RabbitSubscriber>();
            _config = config;
            this.scopeFactory = scopeFactory;
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

            RabbitCommonTools.DeclareExchangesAndQueueForSubscriber(_channel, _config);
            foreach (var queueDescription in _config.NetworkInfos.ServiceQueueDescriptions)
            {
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += OnEventReceived;
                _channel.BasicConsume(
                    queue: queueDescription.QueueName,
                    autoAck: false,
                    consumer: consumer);
                _consumers.Add(consumer);
            }
        }

        /// <summary>
        /// Stop the server and cleanup resources.
        /// </summary>
        public void Stop()
            => Dispose();

        #endregion

        #region Private methods

        private IConnection GetConnection() => _config.ConnectionInfos.ConnectionFactory.CreateConnection();

        private IModel GetChannel(IConnection connection) => connection.CreateModel();

        private async void OnEventReceived(object model, BasicDeliverEventArgs args)
        {
            if (args.Body?.Any() == true && model is EventingBasicConsumer consumer)
            {
                var result = Result.Ok();
                try
                {
                    var dataAsStr = Encoding.UTF8.GetString(args.Body);
                    var enveloppe = dataAsStr.FromJson<Enveloppe>();
                    if (enveloppe != null)
                    {
                        if (enveloppe.Emiter == _config.ConnectionInfos.ServiceName)
                        {
                            consumer.Model.BasicAck(args.DeliveryTag, false);
                            return;
                        }
                        if (!string.IsNullOrWhiteSpace(enveloppe.Data) && !string.IsNullOrWhiteSpace(enveloppe.AssemblyQualifiedDataType))
                        {
                            var objType = Type.GetType(enveloppe.AssemblyQualifiedDataType);
                            if (objType != null)
                            {
                                var serializer = GetSerializerByContentType(args.BasicProperties?.ContentType);
                                if (typeof(IDomainEvent).IsAssignableFrom(objType))
                                {
                                    var evt = serializer.DeserializeEvent(enveloppe.Data, objType);
                                    if (evt != null)
                                    {
                                        try
                                        {
                                            _config.EventCustomCallback?.Invoke(evt);
                                        }
                                        catch (Exception e)
                                        {
                                            _logger.LogError(
                                                $"Error when executing custom callback for event {objType.AssemblyQualifiedName}. {e}");
                                            result = Result.Fail();
                                        }
                                        if (scopeFactory != null)
                                        {
                                            using (var scope = scopeFactory.CreateScope())
                                            {
                                                var bus = scope.Resolve<InMemoryEventBus>();
                                                if (_config.DispatchInMemory && bus != null)
                                                {
                                                    result = await bus.PublishEventAsync(evt).ConfigureAwait(false);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"An event received by RabbitMQ wasn't deserialized. Envelope body : {enveloppe.Data}");
                                    }
                                }
                                else if (typeof(ICommand).IsAssignableFrom(objType))
                                {
                                    var cmd = serializer.DeserializeCommand(enveloppe.Data, objType);
                                    if (cmd != null)
                                    {
                                        try
                                        {
                                            _config.CommandCustomCallback?.Invoke(cmd);
                                        }
                                        catch (Exception e)
                                        {
                                            _logger.LogError(e, $"Error when executing custom callback for command {objType.AssemblyQualifiedName}");
                                            result = Result.Fail();
                                        }
                                        if (scopeFactory != null)
                                        {
                                            using (var scope = scopeFactory.CreateScope())
                                            {
                                                var bus = scope.Resolve<InMemoryCommandBus>();
                                                if (_config.DispatchInMemory && bus != null)
                                                {
                                                    result = await bus.DispatchAsync(cmd).ConfigureAwait(false);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"A command received by RabbitMQ wasn't deserialized. Envelope body : {enveloppe.Data}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogErrorMultilines("RabbitMQServer : Error when treating event.", exc.ToString());
                    result = Result.Fail();
                }
                if (!result && _config.AckStrategy == AckStrategy.AckOnSucces)
                {
                    consumer.Model.BasicReject(args.DeliveryTag, false);
                }
                else
                {
                    consumer.Model.BasicAck(args.DeliveryTag, false);
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

        #region Private methods

        private IDispatcherSerializer GetSerializerByContentType(string? contentType)
            => contentType switch
            {
                _ => new JsonDispatcherSerializer(),
            };

        #endregion

    }
}
