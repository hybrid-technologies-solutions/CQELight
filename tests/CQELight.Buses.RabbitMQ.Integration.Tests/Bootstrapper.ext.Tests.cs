using CQELight.Buses.RabbitMQ.Client;
using CQELight.Buses.RabbitMQ.Publisher;
using CQELight.Buses.RabbitMQ.Server;
using CQELight.Buses.RabbitMQ.Subscriber;
using CQELight.IoC;
using CQELight.TestFramework;
using FluentAssertions;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace CQELight.Buses.RabbitMQ.Integration.Tests
{
    public class BootstrapperExtTests : BaseUnitTestClass
    {
        #region Ctor & members

        #endregion

        #region UseRabbitMQClientBus

        [Fact]
        public void UseRabbitMQClientBus_WithIoC_Should_Add_RabbitMQClient_Within_Registrations()
        {
            new Bootstrapper()
                .UseAutofacAsIoC(c => { })
                .UseRabbitMQClientBus(new RabbitPublisherBusConfiguration("test", "localhost", "guest", "guest"))
                .Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var client = scope.Resolve<RabbitMQClient>();
                client.Should().NotBeNull();
                using (var connection = client.GetConnection())
                {
                    connection.IsOpen.Should().BeTrue();
                }
            }

            using (var connection = RabbitMQClient.Instance.GetConnection())
            {
                connection.IsOpen.Should().BeTrue();
            }

        }

        #endregion

        #region UseRabbitMQServer

        [Fact]
        public void UseRabbitMQServer_WithIoC_Should_Add_RabbitMQClient_Within_Registrations()
        {
            new Bootstrapper()
                .UseAutofacAsIoC(c => { })
                .UseRabbitMQServer(new RabbitMQServerConfiguration("test",
                new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" }, QueueConfiguration.Empty))
                .Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var client = scope.Resolve<RabbitMQClient>();
                client.Should().NotBeNull();
                using (var connection = client.GetConnection())
                {
                    connection.IsOpen.Should().BeTrue();
                }
            }

            using (var connection = RabbitMQClient.Instance.GetConnection())
            {
                connection.IsOpen.Should().BeTrue();
            }
        }

        #endregion
    }
}
