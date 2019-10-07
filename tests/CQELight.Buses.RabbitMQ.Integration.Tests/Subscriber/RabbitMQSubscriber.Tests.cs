using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events;
using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Buses.RabbitMQ.Subscriber;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.Buses.RabbitMQ.Integration.Tests
{
    public class RabbitMQSubscriberTests : BaseUnitTestClass
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

        public RabbitMQSubscriberTests()
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddProvider(new DebugLoggerProvider());
            CleanQueues();
            DeleteData();
        }

        private void DeleteData()
        {
            try
            {
                _channel.ExchangeDelete(firstProducerEventExchangeName);
                _channel.ExchangeDelete(secondProducerEventExchangeName);
                _channel.ExchangeDelete(Consts.CONST_DEAD_LETTER_EXCHANGE_NAME);
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

        private void CreateExchanges()
        {
            _channel.ExchangeDeclare(firstProducerEventExchangeName, "fanout", true, false);
            _channel.ExchangeDeclare(secondProducerEventExchangeName, "fanout", true, false);
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
                var serviceQueue = networkInfos.ServiceQueueDescriptions[0];
                serviceQueue.EventCustomCallback = (e) => eventReceived = e is RabbitEvent;
                serviceQueue.DispatchInMemory = false;

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = RabbitConnectionInfos.FromConnectionFactory(
                        new ConnectionFactory
                        {
                            HostName = "localhost",
                            UserName = "guest",
                            Password = "guest"
                        },
                        "sub1"
                    ),
                    NetworkInfos = networkInfos
                };
                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config,
                    () => eventBus,
                    () => commandBus);

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
                serviceQueue.EventCustomCallback = (e) => eventReceived = e is RabbitEvent;
                serviceQueue.DispatchInMemory = false;
                serviceQueue.Bindings.Add(new RabbitQueueBindingDescription(firstProducerEventExchangeName));

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = RabbitConnectionInfos.FromConnectionFactory(
                        new ConnectionFactory
                        {
                            HostName = "localhost",
                            UserName = "guest",
                            Password = "guest"
                        },
                        "sub1"
                    ),
                    NetworkInfos = networkInfos
                };
                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config,
                    () => eventBus,
                    () => commandBus);

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
                    DispatchInMemory = false,
                    EventCustomCallback = (e) => eventReceived = e is RabbitEvent,
                    Bindings = new System.Collections.Generic.List<RabbitQueueBindingDescription>
                    {
                       new RabbitQueueBindingDescription("MyCustomExchange")
                    }
                });

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = RabbitConnectionInfos.FromConnectionFactory(
                        new ConnectionFactory
                        {
                            HostName = "localhost",
                            UserName = "guest",
                            Password = "guest"
                        },
                        "sub1"
                    ),
                    NetworkInfos = networkInfos
                };
                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config,
                    () => eventBus,
                    () => commandBus);

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
                serviceQueue.CommandCustomCallback = (c) => commandReceived = c is RabbitCommand;
                serviceQueue.DispatchInMemory = false;

                var config = new RabbitSubscriberConfiguration
                {
                    UseDeadLetterQueue = false,
                    ConnectionInfos = RabbitConnectionInfos.FromConnectionFactory(
                        new ConnectionFactory
                        {
                            HostName = "localhost",
                            UserName = "guest",
                            Password = "guest"
                        },
                        "sub1"
                    ),
                    NetworkInfos = networkInfos
                };
                var subscriber = new RabbitSubscriber(
                    _loggerFactory,
                    config,
                    () => eventBus,
                    () => commandBus);

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

        #endregion

        #endregion

    }

    //    #region Start

    //    [Fact]
    //    public async Task RabbitMQSubscriber_Should_Listen_To_AllExistingExchange_WithDefaultConfiguration()
    //    {
    //        try
    //        {
    //            CreateExchanges();
    //            var messages = new List<object>();
    //            var config = RabbitSubscriberConfiguration.GetDefault(subscriber1Name, GetConnectionFactory());
    //            config.ExchangeConfigurations.DoForEach(e =>
    //                e.QueueConfiguration = new QueueConfiguration(new JsonDispatcherSerializer(), subscriber1QueueName, callback: (e) => messages.Add(e)));

    //            var subscriber = new RabbitMQSubscriber(
    //                _loggerFactory,
    //                new RabbitMQSubscriberClientConfiguration(subscriber1Name, GetConnectionFactory(), config));

    //            subscriber.Start();

    //            var eventFromOne = GetEnveloppeDataForEvent(publisher1Name, "test 1 evt");
    //            var cmdFromOne = GetEnveloppeDataForCommand(publisher1Name, "test 1 cmd");
    //            var eventFromTwo = GetEnveloppeDataForEvent(publisher2Name, "test 2 evt");
    //            var cmdFromTwo = GetEnveloppeDataForCommand(publisher2Name, "test 2 cmd");

    //            _channel.BasicPublish(firstProducerEventExchangeName, "", body: eventFromOne);
    //            _channel.BasicPublish(secondProducerEventExchangeName, "", body: eventFromTwo);
    //            _channel.BasicPublish(firstProducerCommandExchangeName, subscriber1Name, body: cmdFromOne);
    //            _channel.BasicPublish(secondProducerCommandExchangeName, subscriber1Name, body: cmdFromTwo);

    //            uint spentTime = 0;
    //            while (messages.Count < 4 && spentTime < 2000)
    //            {
    //                spentTime += 50;
    //                await Task.Delay(50);
    //            }

    //            messages.Should().HaveCount(4);
    //            messages.Count(e => e.GetType() == typeof(RabbitEvent)).Should().Be(2);
    //            messages.Count(e => e.GetType() == typeof(RabbitCommand)).Should().Be(2);

    //        }
    //        finally
    //        {
    //            DeleteData();
    //        }
    //    }

    //    private class RabbitHandler : IDomainEventHandler<RabbitEvent>, IAutoRegisterType
    //    {
    //        public static event Action<RabbitEvent> OnEventArrived;
    //        public Task<Result> HandleAsync(RabbitEvent domainEvent, IEventContext context = null)
    //        {
    //            OnEventArrived?.Invoke(domainEvent);
    //            return Result.Ok();
    //        }
    //    }

    //    [Fact]
    //    public async Task RabbitMQSubscriber_Should_Listen_To_Configuration_Defined_Exchanges_And_Dispatch_InMemory()
    //    {
    //        try
    //        {
    //            CreateExchanges();
    //            var messages = new List<object>();
    //            var config = RabbitSubscriberConfiguration.GetDefault(subscriber1Name, GetConnectionFactory());
    //            config.ExchangeConfigurations.DoForEach(e =>
    //                e.QueueConfiguration =
    //                    new QueueConfiguration(
    //                        new JsonDispatcherSerializer(), subscriber1QueueName,
    //                        dispatchInMemory: true,
    //                        callback: (e) => messages.Add(e)));

    //            var subscriber = new RabbitMQSubscriber(
    //                _loggerFactory,
    //                new RabbitMQSubscriberClientConfiguration(subscriber1Name, GetConnectionFactory(), config),
    //                () => new InMemory.Events.InMemoryEventBus());

    //            subscriber.Start();

    //            var evt = new RabbitEvent { Data = "data" };

    //            _channel.BasicPublish(
    //                firstProducerEventExchangeName,
    //                "",
    //                body: Encoding.UTF8.GetBytes(
    //                        JsonConvert.SerializeObject(
    //                            new Enveloppe(
    //                                JsonConvert.SerializeObject(evt), typeof(RabbitEvent), publisher1Name))));

    //            bool isFired = false;
    //            string data = "";
    //            RabbitHandler.OnEventArrived += (e) =>
    //            {
    //                isFired = true;
    //                data = e.Data;
    //            };
    //            ushort spentTime = 0;
    //            while (!isFired && spentTime < 2000)
    //            {
    //                spentTime += 50;
    //                await Task.Delay(50);
    //            }
    //            isFired.Should().BeTrue();
    //            data.Should().Be("data");
    //        }
    //        finally
    //        {
    //            DeleteData();
    //        }
    //    }

    //    #region AckStrategy

    //    private class AutoAckEvent : BaseDomainEvent { }
    //    private class ExceptionEvent : BaseDomainEvent { }
    //    private class AutoAckEventHandler : IDomainEventHandler<AutoAckEvent>
    //    {
    //        public Task<Result> HandleAsync(AutoAckEvent domainEvent, IEventContext context = null)
    //            => Result.Ok();
    //    }
    //    private class ExceptionEventHandler : IDomainEventHandler<ExceptionEvent>
    //    {
    //        public Task<Result> HandleAsync(ExceptionEvent domainEvent, IEventContext context = null)
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    [Fact]
    //    public async Task RabbitMQSubscriber_Should_Consider_AckStrategy_Ack_On_Success()
    //    {
    //        try
    //        {
    //            CreateExchanges();
    //            var messages = new List<object>();
    //            var config = RabbitSubscriberConfiguration.GetDefault(subscriber1Name, GetConnectionFactory());
    //            config.ExchangeConfigurations.DoForEach(e =>
    //                e.QueueConfiguration =
    //                    new QueueConfiguration(
    //                        new JsonDispatcherSerializer(), publisher1AckQueueName,
    //                        dispatchInMemory: true,
    //                        callback: (e) => messages.Add(e),
    //                        createAndUseDeadLetterQueue: true));
    //            config.AckStrategy = AckStrategy.AckOnSucces;
    //            var bus = new InMemory.Events.InMemoryEventBus(new InMemory.Events.InMemoryEventBusConfiguration
    //            {
    //                NbRetries = 0
    //            });

    //            var subscriber = new RabbitMQSubscriber(
    //                _loggerFactory,
    //                new RabbitMQSubscriberClientConfiguration(subscriber1Name, GetConnectionFactory(), config),
    //                () => bus);

    //            subscriber.Start();

    //            var evt = new AutoAckEvent();

    //            _channel.BasicPublish(
    //                firstProducerEventExchangeName,
    //                "",
    //                body: Encoding.UTF8.GetBytes(
    //                        JsonConvert.SerializeObject(
    //                            new Enveloppe(
    //                                JsonConvert.SerializeObject(evt), typeof(AutoAckEvent), publisher1Name))));
    //            await Task.Delay(100);
    //            var result = _channel.BasicGet(Consts.CONST_DEAD_LETTER_QUEUE_NAME, true);
    //            result.Should().BeNull();
    //        }
    //        finally
    //        {
    //            DeleteData();
    //        }
    //    }

    //    [Fact]
    //    public async Task RabbitMQSubscriber_Should_Consider_AckStrategy_Ack_On_Success_Fail_Should_Move_To_DLQ()
    //    {
    //        try
    //        {
    //            CreateExchanges();
    //            var messages = new List<object>();
    //            var config = RabbitSubscriberConfiguration.GetDefault(subscriber1Name, GetConnectionFactory());
    //            config.ExchangeConfigurations.DoForEach(e =>
    //                e.QueueConfiguration =
    //                    new QueueConfiguration(
    //                        new JsonDispatcherSerializer(), publisher1AckQueueName,
    //                        dispatchInMemory: true,
    //                        callback: (e) => messages.Add(e),
    //                        createAndUseDeadLetterQueue: true));
    //            config.AckStrategy = AckStrategy.AckOnSucces;

    //            var bus = new InMemory.Events.InMemoryEventBus(new InMemory.Events.InMemoryEventBusConfiguration
    //            {
    //                NbRetries = 0
    //            });

    //            var subscriber = new RabbitMQSubscriber(
    //                _loggerFactory,
    //                new RabbitMQSubscriberClientConfiguration(subscriber1Name, GetConnectionFactory(), config),
    //                () => bus);

    //            subscriber.Start();

    //            var evt = new ExceptionEvent();

    //            _channel.BasicPublish(
    //                firstProducerEventExchangeName,
    //                "",
    //                body: Encoding.UTF8.GetBytes(
    //                        JsonConvert.SerializeObject(
    //                            new Enveloppe(
    //                                JsonConvert.SerializeObject(evt), typeof(ExceptionEvent), publisher1Name))));
    //            await Task.Delay(250);
    //            var result = _channel.BasicGet(Consts.CONST_DEAD_LETTER_QUEUE_NAME, true);
    //            result.Should().NotBeNull();
    //        }
    //        finally
    //        {
    //            DeleteData();
    //        }
    //    }

    //    [Fact]
    //    public async Task RabbitMQSubscriber_Should_Consider_AckStrategy_Ack_On_Receive_Fail_Should_Remove_MessageFromQueue()
    //    {
    //        try
    //        {
    //            CreateExchanges();
    //            var messages = new List<object>();
    //            var config = RabbitSubscriberConfiguration.GetDefault(subscriber1Name, GetConnectionFactory());
    //            config.ExchangeConfigurations.DoForEach(e =>
    //                e.QueueConfiguration =
    //                    new QueueConfiguration(
    //                        new JsonDispatcherSerializer(), publisher1AckQueueName,
    //                        dispatchInMemory: true,
    //                        callback: (e) => messages.Add(e),
    //                        createAndUseDeadLetterQueue: true));
    //            config.AckStrategy = AckStrategy.AckOnReceive;
    //            var bus = new InMemory.Events.InMemoryEventBus(new InMemory.Events.InMemoryEventBusConfiguration
    //            {
    //                NbRetries = 0
    //            });
    //            var subscriber = new RabbitMQSubscriber(
    //                _loggerFactory,
    //                new RabbitMQSubscriberClientConfiguration(subscriber1Name, GetConnectionFactory(), config),
    //                () => bus);

    //            subscriber.Start();

    //            var evt = new ExceptionEvent();

    //            _channel.BasicPublish(
    //                firstProducerEventExchangeName,
    //                "",
    //                body: Encoding.UTF8.GetBytes(
    //                        JsonConvert.SerializeObject(
    //                            new Enveloppe(
    //                                JsonConvert.SerializeObject(evt), typeof(ExceptionEvent), publisher1Name))));
    //            await Task.Delay(250);
    //            var result = _channel.BasicGet(Consts.CONST_DEAD_LETTER_QUEUE_NAME, true);
    //            result.Should().BeNull();
    //        }
    //        finally
    //        {
    //            DeleteData();
    //        }
    //    }

    //    #endregion

    //}

    //#endregion

}


