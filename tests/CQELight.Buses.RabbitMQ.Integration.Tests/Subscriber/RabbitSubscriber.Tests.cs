using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Buses.RabbitMQ.Subscriber;
using CQELight.TestFramework;
using CQELight.TestFramework.IoC;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.Buses.RabbitMQ.Integration.Tests
{
    public class RabbitSubscriberTests : BaseUnitTestClass
    {
        #region Ctor & members

        private class RabbitEvent : BaseDomainEvent
        {
            public string Data { get; set; }
        }
        private class RabbitCommand : ICommand
        {
            public string Data { get; set; }
        }

        private readonly ILoggerFactory _loggerFactory;
        private IModel _channel;

        const string subscriber1Name = "sub1";
        const string subscriber2Name = "sub2";

        const string publisher1Name = "prod1";
        const string publisher2Name = "prod2";

        const string firstProducerEventExchangeName = publisher1Name + "_events";
        const string secondProducerEventExchangeName = publisher2Name + "_events";

        const string publisher1QueueName = publisher1Name + "_queue";
        const string publisher2QueueName = publisher2Name + "_queue";
        const string publisher1AckQueueName = publisher1Name + "_ack_queue";
        const string publisher2AckQueueName = publisher2Name + "_ack_queue";

        const string subscriber1QueueName = subscriber1Name + "_queue";
        const string subscriber2QueueName = subscriber2Name + "_queue";

        private readonly InMemoryEventBus eventBus = new InMemoryEventBus();
        private readonly InMemoryCommandBus commandBus = new InMemoryCommandBus();

        private readonly TestScopeFactory scopeFactory;

        public RabbitSubscriberTests()
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddProvider(new DebugLoggerProvider());
            CleanQueues();
            DeleteData();
            scopeFactory = new TestScopeFactory(new TestScope(new Dictionary<Type, object>
            {
                { typeof(InMemoryEventBus), eventBus },
                {typeof(InMemoryCommandBus), commandBus } }
            ));
        }

        ~RabbitSubscriberTests()
        {
            _channel.Dispose();
        }

        private void DeleteData()
        {
            try
            {
                _channel.ExchangeDelete(firstProducerEventExchangeName);
                _channel.ExchangeDelete(secondProducerEventExchangeName);
                _channel.ExchangeDelete("sub1_exchange");
                _channel.ExchangeDelete(Consts.CONST_DEAD_LETTER_EXCHANGE_NAME);
                _channel.ExchangeDelete(Consts.CONST_CQE_EXCHANGE_NAME);
                _channel.QueueDelete(publisher1QueueName);
                _channel.QueueDelete(publisher2QueueName);
                _channel.QueueDelete(subscriber1QueueName);
                _channel.QueueDelete(subscriber2QueueName);
                _channel.QueueDelete(publisher1AckQueueName);
                _channel.QueueDelete(publisher2AckQueueName);
                _channel.QueueDelete(Consts.CONST_DEAD_LETTER_QUEUE_NAME);
            }
            catch { }
        }

        private ConnectionFactory GetConnectionFactory()
            => new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

        private void CleanQueues()
        {
            var factory = GetConnectionFactory();
            var connection = factory.CreateConnection();

            _channel = connection.CreateModel();
        }

        private byte[] GetEnveloppeDataForEvent(string publisher, string content)
        {
            var evt = new RabbitEvent { Data = content };
            var ev = new Enveloppe(JsonConvert.SerializeObject(evt), typeof(RabbitEvent), publisher);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ev));
        }

        private byte[] GetEnveloppeDataForCommand(string publisher, string content)
        {
            var evt = new RabbitCommand { Data = content };
            var ev = new Enveloppe(JsonConvert.SerializeObject(evt), typeof(RabbitCommand), publisher);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ev));
        }

        #endregion

        #region Start

        #region Events

        [Fact]
        public async Task OneExchange_Network_Configuration_AsExpected_Event()
        {
            try
            {
                bool eventReceived = false;
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("sub1", RabbitMQExchangeStrategy.SingleExchange);
                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    DispatchInMemory = false
                };
                config.EventCustomCallback = (e) => eventReceived = e is RabbitEvent;
                var subscriber = new RabbitSubscriber(
                        _loggerFactory,
                        config,
                        scopeFactory);

                subscriber.Start();

                var enveloppeWithFirstEvent = GetEnveloppeDataForEvent(publisher: "pub1", content: "data");

                _channel.BasicPublish(
                    exchange: Consts.CONST_CQE_EXCHANGE_NAME,
                    routingKey: "",
                    basicProperties: null,
                    body: enveloppeWithFirstEvent);

                int awaitedTime = 0;
                while (awaitedTime <= 2000)
                {
                    if (eventReceived) break;
                    await Task.Delay(10);
                    awaitedTime += 10;
                }

                eventReceived.Should().BeTrue();
            }
            finally
            {
                DeleteData();
            }
        }

        [Fact]
        public async Task OneExchangePerService_NetworkConfiguration_AsExpected_Event()
        {
            try
            {
                bool eventReceived = false;
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("sub1", RabbitMQExchangeStrategy.ExchangePerService);

                networkInfos.DistantExchangeDescriptions.Add(
                        new RabbitExchangeDescription(firstProducerEventExchangeName)
                    );

                var serviceQueue = networkInfos.ServiceQueueDescriptions[0];
                serviceQueue.Bindings.Add(new RabbitQueueBindingDescription(firstProducerEventExchangeName));

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    DispatchInMemory = false
                };
                config.EventCustomCallback = (e) => eventReceived = e is RabbitEvent;
                var subscriber = new RabbitSubscriber(
                        _loggerFactory,
                        config,
                        scopeFactory);

                subscriber.Start();

                var enveloppeWithFirstEvent = GetEnveloppeDataForEvent(publisher: "pub1", content: "data");

                _channel.BasicPublish(
                    exchange: firstProducerEventExchangeName,
                    routingKey: "",
                    basicProperties: null,
                    body: enveloppeWithFirstEvent);

                int awaitedTime = 0;
                while (awaitedTime <= 2000)
                {
                    if (eventReceived) break;
                    await Task.Delay(10);
                    awaitedTime += 10;
                }

                eventReceived.Should().BeTrue();
            }
            finally
            {
                DeleteData();
            }
        }

        [Fact]
        public async Task CustomNetworkConfig_AsExpected_Event()
        {
            try
            {
                bool eventReceived = false;
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("sub1", RabbitMQExchangeStrategy.Custom);

                networkInfos.DistantExchangeDescriptions.Add(
                        new RabbitExchangeDescription("MyCustomExchange")
                    );

                networkInfos.ServiceQueueDescriptions.Add(new RabbitQueueDescription("MyCustomQueue")
                {
                    Bindings = new System.Collections.Generic.List<RabbitQueueBindingDescription>
                    {
                       new RabbitQueueBindingDescription("MyCustomExchange")
                    }
                });

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    DispatchInMemory = false,
                };
                config.EventCustomCallback = (e) => eventReceived = e is RabbitEvent;
                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config,
                    scopeFactory);

                subscriber.Start();

                var enveloppeWithFirstEvent = GetEnveloppeDataForEvent(publisher: "pub1", content: "data");

                _channel.BasicPublish(
                    exchange: "MyCustomExchange",
                    routingKey: "",
                    basicProperties: null,
                    body: enveloppeWithFirstEvent);

                int awaitedTime = 0;
                while (awaitedTime <= 2000)
                {
                    if (eventReceived) break;
                    await Task.Delay(10);
                    awaitedTime += 10;
                }

                eventReceived.Should().BeTrue();
            }
            finally
            {
                DeleteData();
                _channel.ExchangeDelete("MyCustomExchange");
                _channel.QueueDelete("MyCustomQueue");
            }
        }

        #endregion

        #region Command

        [Fact]
        public async Task Command_Should_Be_Send_AsDirect()
        {
            try
            {
                bool commandReceived = false;
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("sub1", RabbitMQExchangeStrategy.SingleExchange);
                var serviceQueue = networkInfos.ServiceQueueDescriptions[0];
                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    DispatchInMemory = false
                };
                config.CommandCustomCallback = (c) => commandReceived = c is RabbitCommand;

                var subscriber = new RabbitSubscriber(
                        _loggerFactory,
                        config,
                        scopeFactory);

                subscriber.Start();

                var enveloppeWithCommand = GetEnveloppeDataForCommand(publisher: "pub1", content: "data");

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: serviceQueue.QueueName,
                    basicProperties: null,
                    body: enveloppeWithCommand);

                int awaitedTime = 0;
                while (awaitedTime <= 2000)
                {
                    if (commandReceived) break;
                    await Task.Delay(10);
                    awaitedTime += 10;
                }

                commandReceived.Should().BeTrue();
            }
            finally
            {
                DeleteData();
            }
        }

        private static RabbitConnectionInfos GetConnectionInfos()
        {
            return RabbitConnectionInfos.FromConnectionFactory(
                                    new ConnectionFactory
                                    {
                                        HostName = "localhost",
                                        UserName = "guest",
                                        Password = "guest"
                                    },
                                    "sub1"
                                );
        }

        #endregion

        #region AckStrategy

        private class AutoAckEvent : BaseDomainEvent { }
        private class ExceptionEvent : BaseDomainEvent { }

        [Fact]
        public async Task RabbitSubscriber_Should_Consider_AckStrategy_Ack_On_Success_CallbackExc()
        {
            try
            {
                bool eventReceived = false;
                var messages = new List<object>();
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("sub1", RabbitMQExchangeStrategy.SingleExchange);

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = true,
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    DispatchInMemory = false
                };
                config.EventCustomCallback = (e) => { messages.Add(e); eventReceived = true; };

                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config);

                subscriber.Start();

                var evt = new AutoAckEvent();

                _channel.BasicPublish(
                    Consts.CONST_CQE_EXCHANGE_NAME,
                    "",
                    body: Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                new Enveloppe(
                                    JsonConvert.SerializeObject(evt), typeof(AutoAckEvent), publisher1Name))));
                int awaitedTime = 0;
                while (awaitedTime <= 2000)
                {
                    if (eventReceived) break;
                    await Task.Delay(10);
                    awaitedTime += 10;
                }
                eventReceived.Should().BeTrue();
                var result = _channel.BasicGet(Consts.CONST_DEAD_LETTER_QUEUE_NAME, true);
                result.Should().BeNull();
            }
            finally
            {
                DeleteData();
            }
        }

        [Fact]
        public async Task RabbitMQSubscriber_Should_Consider_AckStrategy_Ack_On_Success_Fail_Should_Move_To_DLQ_CallbackExc()
        {
            try
            {
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("sub1", RabbitMQExchangeStrategy.SingleExchange);

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = true,
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos
                };
                config.EventCustomCallback += (_) => throw new InvalidOperationException();

                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config);

                subscriber.Start();

                var evt = new ExceptionEvent();

                _channel.BasicPublish(
                    Consts.CONST_CQE_EXCHANGE_NAME,
                    "",
                    body: Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                new Enveloppe(
                                    JsonConvert.SerializeObject(evt), typeof(ExceptionEvent), publisher1Name))));
                int awaitedTime = 0;
                BasicGetResult result = null;
                while (awaitedTime <= 2000)
                {
                    result = _channel.BasicGet(Consts.CONST_DEAD_LETTER_QUEUE_NAME, true);
                    if (result != null) break;
                    await Task.Delay(10);
                    awaitedTime += 10;
                }
                result.Should().NotBeNull();
            }
            finally
            {
                DeleteData();
            }
        }

        [Fact]
        public async Task RabbitMQSubscriber_Should_Consider_AckStrategy_Ack_On_Receive_Fail_Should_Remove_MessageFromQueue_CallbackExc()
        {
            try
            {
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("sub1", RabbitMQExchangeStrategy.SingleExchange);

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = true,
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    AckStrategy = AckStrategy.AckOnReceive
                };
                config.EventCustomCallback += (_) => throw new InvalidOperationException();

                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config);

                subscriber.Start();

                var evt = new ExceptionEvent();

                _channel.BasicPublish(
                    Consts.CONST_CQE_EXCHANGE_NAME,
                    "",
                    body: Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(
                                new Enveloppe(
                                    JsonConvert.SerializeObject(evt), typeof(ExceptionEvent), publisher1Name))));
                await Task.Delay(250);
                int awaitedTime = 0;
                BasicGetResult result = null;
                while (awaitedTime <= 750)
                {
                    result = _channel.BasicGet(Consts.CONST_DEAD_LETTER_QUEUE_NAME, true);
                    if (result != null) break;
                    await Task.Delay(10);
                    awaitedTime += 10;
                }
                result.Should().BeNull();
            }
            finally
            {
                DeleteData();
            }
        }

        #endregion

        #endregion

    }
}


