using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events;
using CQELight.Buses.RabbitMQ.Common;
using CQELight.Buses.RabbitMQ.Common.Abstractions;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Buses.RabbitMQ.Publisher;
using CQELight.TestFramework;
using CQELight.Tools.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Moq;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.Buses.RabbitMQ.Integration.Tests.Publisher
{
    public class RabbitPublisherTests : BaseUnitTestClass
    {
        #region Ctor & members

        private IModel channel;
        private ILoggerFactory loggerFactory;

        private class TestCommand : ICommand { }
        private class TestEvent : BaseDomainEvent { }

        public RabbitPublisherTests()
        {
            loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new DebugLoggerProvider());
            CreateChannel();
        }

        ~RabbitPublisherTests()
        {
            channel.Dispose();
        }

        private void DeleteData()
        {
            channel.QueueDelete("CQELight");
            channel.ExchangeDelete("pub1_exchange");
        }

        private ConnectionFactory GetConnectionFactory() =>
           new ConnectionFactory()
           {
               HostName = "localhost",
               UserName = "guest",
               Password = "guest"
           };

        private void CreateChannel()
        {
            var factory = GetConnectionFactory();
            var connection = factory.CreateConnection();

            channel = connection.CreateModel();
        }

        private RabbitConnectionInfos GetConnectionInfos()
                => RabbitConnectionInfos.FromConnectionFactory(new ConnectionFactory
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
                }, "pub1");

        #endregion

        #region DispatchAsync

        [Fact]
        public async Task DispatchAsync_Should_Sent_To_FirstNamespacePart_By_Convention()
        {
            try
            {

                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("CQELight", RabbitMQExchangeStrategy.Custom);
                networkInfos.ServiceQueueDescriptions.Add(new RabbitQueueDescription("CQELight"));
                var config = new RabbitPublisherConfiguration
                {
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos
                };
                var publisher = new RabbitPublisher(
                    loggerFactory,
                    config);

                await publisher.DispatchAsync(new TestCommand());

                var result = channel.BasicGet("CQELight", true);
                result.Should().NotBeNull();
                Encoding.UTF8.GetString(result.Body.ToArray()).FromJson<TestCommand>().Should().NotBeNull();
            }
            finally
            {
                DeleteData();
            }
        }

        #endregion

        #region PublishAsync

        [Fact]
        public async Task PublishAsync_Should_Sent_To_Service_Exchange()
        {
            try
            {
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("CQELight", RabbitMQExchangeStrategy.Custom);
                networkInfos.ServiceExchangeDescriptions.Add(new RabbitExchangeDescription("pub1_exchange"));
                var config = new RabbitPublisherConfiguration
                {
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos
                };
                RabbitCommonTools.DeclareExchangesAndQueueForPublisher(channel, config);
                channel.QueueDeclare("CQELight");
                channel.QueueBind("CQELight", "pub1_exchange", "");

                var publisher = new RabbitPublisher(
                    loggerFactory,
                    config);

                await publisher.PublishEventAsync(new TestEvent());

                var result = channel.BasicGet("CQELight", true);
                result.Should().NotBeNull();
                Encoding.UTF8.GetString(result.Body.ToArray()).FromJson<TestCommand>().Should().NotBeNull();
            }
            finally
            {
                DeleteData();
            }
        }

        #endregion

        #region RoutingKeyFactory

        [Fact]
        public async Task DispatchAsync_Default_Routing_Key_Factory_Should_Use_First_Namespace_Part()
        {
            try
            {
                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("CQELight", RabbitMQExchangeStrategy.Custom);
                networkInfos.ServiceQueueDescriptions.Add(new RabbitQueueDescription("CQELight"));
                var config = new RabbitPublisherConfiguration
                {
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    RoutingKeyFactory = new RabbitDefaultRoutingKeyFactory()
                };
                var publisher = new RabbitPublisher(
                    loggerFactory,
                    config);

                await publisher.DispatchAsync(new TestCommand());

                var result = channel.BasicGet("CQELight", true);
                result.Should().NotBeNull();
                Encoding.UTF8.GetString(result.Body.ToArray()).FromJson<TestCommand>().Should().NotBeNull();
            }
            finally
            {
                DeleteData();
            }
        }

        [Fact]
        public async Task DispatchAsync_Default_Routing_Key_Factory_Should_Be_Considered()
        {
            try
            {
                var fakeRoutingKeyFactory = new Mock<IRoutingKeyFactory>();
                fakeRoutingKeyFactory
                    .Setup(m => m.GetRoutingKeyForCommand(It.IsAny<object>()))
                    .Returns("MyCustomQueue");

                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("CQELight", RabbitMQExchangeStrategy.Custom);
                networkInfos.ServiceQueueDescriptions.Add(new RabbitQueueDescription("CQELight"));
                networkInfos.ServiceQueueDescriptions.Add(new RabbitQueueDescription("MyCustomQueue"));
                var config = new RabbitPublisherConfiguration
                {
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    RoutingKeyFactory = fakeRoutingKeyFactory.Object
                };
                var publisher = new RabbitPublisher(
                    loggerFactory,
                    config);

                await publisher.DispatchAsync(new TestCommand());

                channel.BasicGet("CQELight", true).Should().BeNull();

                var result = channel.BasicGet("MyCustomQueue", true);
                result.Should().NotBeNull();
                Encoding.UTF8.GetString(result.Body.ToArray()).FromJson<TestCommand>().Should().NotBeNull();
            }
            finally
            {
                DeleteData();
                channel.QueueDelete("MyCustomQueue");
            }
        }

        [Fact]
        public async Task PublishAsync_RoutingKey_Topic_Should_BeConsidered()
        {
            try
            {
                var fakeRoutingKeyFactory = new Mock<IRoutingKeyFactory>();
                fakeRoutingKeyFactory
                    .Setup(m => m.GetRoutingKeyForEvent(It.IsAny<object>()))
                    .Returns("cqelight.events.testevent");

                var networkInfos = RabbitNetworkInfos.GetConfigurationFor("CQELight", RabbitMQExchangeStrategy.Custom);
                networkInfos.ServiceQueueDescriptions.Add(new RabbitQueueDescription("CQELight"));
                networkInfos.ServiceExchangeDescriptions.Add(new RabbitExchangeDescription("MyCustomExchange")
                {
                    ExchangeType = ExchangeType.Topic
                });
                var queue = new RabbitQueueDescription("MyCustomQueue");
                queue.Bindings.Add(new RabbitQueueBindingDescription("MyCustomExchange")
                {
                    RoutingKeys = new List<string> { "*.events.*" }
                });
                networkInfos.ServiceQueueDescriptions.Add(queue);
                var config = new RabbitPublisherConfiguration
                {
                    ConnectionInfos = GetConnectionInfos(),
                    NetworkInfos = networkInfos,
                    RoutingKeyFactory = fakeRoutingKeyFactory.Object
                };
                var publisher = new RabbitPublisher(
                    loggerFactory,
                    config);

                await publisher.PublishEventAsync(new TestEvent());

                channel.BasicGet("CQELight", true).Should().BeNull();

                var result = channel.BasicGet("MyCustomQueue", true);
                result.Should().NotBeNull();
                Encoding.UTF8.GetString(result.Body.ToArray()).FromJson<TestEvent>().Should().NotBeNull();
            }
            finally
            {
                DeleteData();
                channel.QueueDelete("MyCustomQueue");
                channel.ExchangeDelete("MyCustomExchange");
            }
        }

        #endregion
    }
}
