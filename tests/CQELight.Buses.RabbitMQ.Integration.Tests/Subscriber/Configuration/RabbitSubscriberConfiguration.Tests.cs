using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace CQELight.Buses.RabbitMQ.Integration.Tests.Subscriber.Configuration
{
    public class RabbitSubscriberConfigurationTests
    {
        #region Ctor & members

        private IConfiguration _testConfiguration;
        private IModel _channel;

        const string FirstExchange = "First_Test_Exchange";
        const string SecondExchange = "Second_Test_Exchange";

        public RabbitSubscriberConfigurationTests()
        {
            _testConfiguration = new ConfigurationBuilder().AddJsonFile("test-config.json").Build();

            CreateChannel();
            DeleteData();
        }

        private void DeleteData()
        {
            _channel.ExchangeDelete(FirstExchange);
            _channel.ExchangeDelete(SecondExchange);
        }

        private void CreateChannel()
        {
            var factory = GetConnectionFactory();
            var connection = factory.CreateConnection();

            _channel = connection.CreateModel();
        }

        private ConnectionFactory GetConnectionFactory() =>
            new ConnectionFactory()
            {
                HostName = _testConfiguration["host"],
                UserName = _testConfiguration["user"],
                Password = _testConfiguration["password"]
            };


        #endregion

        #region Default configuration

        //[Fact]
        //public void RabbitSubscriberConfiguration_Defuaut_Should_Bind_To_AllExchanges_With_Dedicated_Queue()
        //{
        //    try
        //    {
        //        _channel.ExchangeDeclare(
        //                       exchange: FirstExchange,
        //                       type: ExchangeType.Fanout,
        //                       durable: true,
        //                       autoDelete: false);
        //        _channel.ExchangeDeclare(
        //                       exchange: SecondExchange,
        //                       type: ExchangeType.Topic,
        //                       durable: true,
        //                       autoDelete: false);

        //        var configuration = RabbitSubscriberConfiguration.GetDefault("subscriber", GetConnectionFactory());

        //        configuration.ExchangeConfigurations.Should().HaveCount(2);
        //        configuration.ExchangeConfigurations
        //            .Any(e => e.ExchangeDetails.ExchangeName == FirstExchange
        //                   && e.QueueName == "subscriber_queue"
        //                   && e.ExchangeDetails.ExchangeType == "fanout").Should().BeTrue();
        //        configuration.ExchangeConfigurations
        //            .Any(e => e.ExchangeDetails.ExchangeName == SecondExchange
        //                   && e.QueueName == "subscriber_queue"
        //                   && e.ExchangeDetails.ExchangeType == "topic").Should().BeTrue();
        //    }
        //    finally
        //    {
        //        DeleteData();
        //    }
        //}

        #endregion
    }
}
