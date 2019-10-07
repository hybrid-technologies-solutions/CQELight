using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Events.Serializers;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using CQELight.Tools.Extensions;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CQELight.Buses.RabbitMQ.Configuration.Publisher;
using CQELight.Buses.RabbitMQ.Publisher;

namespace CQELight.Buses.RabbitMQ.Integration.Tests
{
    public class RabbitMQCommandBusTests : BaseUnitTestClass
    {
        #region Ctor & members

        private class RabbitCommand : ICommand
        {
            public string Data { get; set; }
        }

        private IModel _channel;
        private readonly IConfiguration _testConfiguration;
        const string queueName = "cqelight_rabbit_test_publish_queue_1";
        const string specificQueueName = "cqelight_rabbit_test_specific_queue_1";
        const string specificExchangeName = "CQELight_Specific_Exchange";
        const string defaultExchangeName = "CQELight_Test_commands";

        public RabbitMQCommandBusTests()
        {
            CreateChannel();
            DeleteData();
        }

        private void DeleteData()
        {
            _channel.ExchangeDelete(defaultExchangeName);
            _channel.ExchangeDelete(specificExchangeName);
            _channel.QueueDelete(queueName);
            _channel.QueueDelete(specificQueueName);
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

            _channel = connection.CreateModel();
        }

        private void DeclareStandardExchangeAndQueue()
        {
            _channel.ExchangeDeclare(
                                exchange: "CQELight_Test_commands",
                                type: ExchangeType.Topic,
                                durable: true,
                                autoDelete: false);
            _channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false);
            _channel.QueueBind(queueName, "CQELight_Test_commands", "CQELight");
        }

        #endregion

        #region PublishEventAsync

        [Fact]
        public async Task RabbitMQCommandBus_DispatchCommandAsync_DefaultConfig_Should_Publish_To_Exchange()
        {
            try
            {
                DeclareStandardExchangeAndQueue();

                var cmd = new RabbitCommand
                {
                    Data = "testData"
                };

                var b = new RabbitMQCommandBus(
                    new JsonDispatcherSerializer(),
                    new RabbitPublisherBusConfiguration("CQELight_Test", GetConnectionFactory()));

                var allCalled = false;

                await b.DispatchAsync(cmd).ContinueWith(t =>
                {
                    t.Result.IsSuccess.Should().BeTrue();
                    var result = _channel.BasicGet(queueName, true);
                    result.Should().NotBeNull();
                    var enveloppeAsStr = Encoding.UTF8.GetString(result.Body);
                    enveloppeAsStr.Should().NotBeNullOrWhiteSpace();

                    var receivedEnveloppe = enveloppeAsStr.FromJson<Enveloppe>();
                    receivedEnveloppe.Should().NotBeNull();

                    var type = Type.GetType(receivedEnveloppe.AssemblyQualifiedDataType);
                    var evet = receivedEnveloppe.Data.FromJson(type);
                    evet.Should().BeOfType<RabbitCommand>();
                    evet.As<RabbitCommand>().Data.Should().Be("testData");
                    allCalled = true;
                }).ConfigureAwait(false);

                allCalled.Should().BeTrue();
            }
            finally
            {
                DeleteData();
            }
        }

        [Fact]
        public async Task RabbitMQEventBus_PublishEventAsync_Should_RespectConfiguration()
        {
            try
            {
                var configBuilder = new RabbitPublisherConfigurationBuilder();
                configBuilder.ForCommand<RabbitCommand>().UseExchange(specificExchangeName);

                DeclareStandardExchangeAndQueue();
                _channel.ExchangeDeclare(
                    exchange: specificExchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false);
                _channel.QueueDeclare(specificQueueName, durable: false, exclusive: false, autoDelete: false);
                _channel.QueueBind(specificQueueName, specificExchangeName, "");

                var cmd = new RabbitCommand
                {
                    Data = "testData"
                };

                var b = new RabbitMQCommandBus(
                    new JsonDispatcherSerializer(),
                    new RabbitPublisherBusConfiguration("CQELight_Test", GetConnectionFactory(), configBuilder.GetConfiguration()));

                var allCalled = false;

                var result = await b.DispatchAsync(cmd);
                result.IsSuccess.Should().BeTrue();

                _channel.BasicGet(queueName, true).Should().BeNull();

                var data = _channel.BasicGet(specificQueueName, true);
                data.Should().NotBeNull();
                var enveloppeAsStr = Encoding.UTF8.GetString(data.Body);
                enveloppeAsStr.Should().NotBeNullOrWhiteSpace();

                var receivedEnveloppe = enveloppeAsStr.FromJson<Enveloppe>();
                receivedEnveloppe.Should().NotBeNull();

                var type = Type.GetType(receivedEnveloppe.AssemblyQualifiedDataType);
                var evet = receivedEnveloppe.Data.FromJson(type);
                evet.Should().BeOfType<RabbitCommand>();
                evet.As<RabbitCommand>().Data.Should().Be("testData");
                allCalled = true;

                allCalled.Should().BeTrue();
            }
            finally
            {
                DeleteData();
            }

        }

        #endregion

    }
}
