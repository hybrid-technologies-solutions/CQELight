using BenchmarkDotNet.Attributes;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.RabbitMQ.Client;
using CQELight.Buses.RabbitMQ.Publisher;
using CQELight.Events.Serializers;
using CQELight_Benchmarks.Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQELight_Benchmarks.Benchmarks.Buses
{
    public class RabbitMQEventBusBenchmark
    {

        #region Setup

        [IterationCleanup]
        public void Cleanup()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
            using (var connection = factory.CreateConnection())
            {
                string queueName = "cqe_appqueue_A0165D77-E5C4-4B9B-A0D5-002163F477C0".ToLower();
                using (var channel = connection.CreateModel())
                {
                    channel.QueuePurge(queueName);
                }
            }
        }

        #endregion

        #region Public methods

        [Benchmark] // We add those benchmark that are mainly the same as command cause we must not see any perf differences between these two
        public async Task DispatchEvent()
        {
            var bus = new RabbitMQEventBus(new JsonDispatcherSerializer(),
                new RabbitPublisherBusConfiguration("benchmark", "localhost", "guest", "guest"));
            await bus.PublishEventAsync(new TestDispatchEvent(0, false, 0));
        }

        [Benchmark]
        [Arguments(50, false)]
        [Arguments(50, true)]
        [Arguments(100, false)]
        [Arguments(100, true)]
        [Arguments(250, true)]
        [Arguments(250, false)]
        [Arguments(1000, true)]
        [Arguments(1000, false)]
        [Arguments(2500, true)]
        [Arguments(2500, false)]
        public async Task DispatchRangeEvents_SameEvent(int nbEvents, bool allowParallelDispatch)
        {

            var bus = new RabbitMQEventBus(new JsonDispatcherSerializer(),
                new RabbitPublisherBusConfiguration("benchmark", "localhost", "guest", "guest",
                parallelDispatchEventTypes: allowParallelDispatch ? new List<Type> { typeof(TestDispatchEvent) } : new List<Type>()));
            var events = new List<IDomainEvent>();
            for (int i = 0; i < nbEvents; i++)
            {
                events.Add(new TestDispatchEvent(i, false, 0));
            }
            await bus.PublishEventRangeAsync(events);
        }

        #endregion

    }
}
