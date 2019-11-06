using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.InMemory.Events;
using CQELight.Dispatcher;
using CQELight.Dispatcher.Configuration;
using CQELight.TestFramework;
using CQELight.TestFramework.IoC;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.Integration.Tests.Dispatcher
{
    public class BaseDispatcherTests : BaseUnitTestClass
    {

        #region Ctor & members

        #endregion

        #region DispatchEventAsync

        private class TestEvent : BaseDomainEvent { }

        [Fact]
        public async Task BaseDispatcher_DispatchEventAsync_Should_Not_Throw_Exception_If_NoConfiguration_IsDefined_And_LogWarning()
        {
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns(loggerMock.Object);
            var fakeScopeFactory = new TestScopeFactory(new TestScope(new Dictionary<Type, object>
            {
                {typeof(ILoggerFactory), loggerFactoryMock.Object }
            }));

            bool coreDispatcherCalledWithoutSecurityCritical = false;

            var d = new BaseDispatcher(new CQELight.Dispatcher.Configuration.DispatcherConfiguration(false), fakeScopeFactory);

            var evt = new TestEvent();

            CoreDispatcher.OnEventDispatched += (e) =>
            {
                coreDispatcherCalledWithoutSecurityCritical = object.ReferenceEquals(e, evt);
                return Task.CompletedTask;
            };

            //Shouldn't throw exception
            await d.PublishEventAsync(evt).ConfigureAwait(false);

            coreDispatcherCalledWithoutSecurityCritical.Should().BeTrue();
        }

        private class DataHolder
        {
            public Dictionary<Guid, string> Data { get; set; }
            = new Dictionary<Guid, string>();
        }
        private class EventOne : BaseDomainEvent
        {
            public EventOne(DataHolder data, Guid aggId)
            {
                Data = data;
                AggregateId = aggId;
            }

            public DataHolder Data { get; set; }
        }
        private class EventTwo : BaseDomainEvent
        {
            public EventTwo(DataHolder data, Guid aggId)
            {
                Data = data;
                AggregateId = aggId;
            }

            public DataHolder Data { get; set; }
        }
        private class EventThree : BaseDomainEvent
        {
            public EventThree(DataHolder data, Guid aggId)
            {
                Data = data;
                AggregateId = aggId;
            }

            public DataHolder Data { get; set; }
        }

        private class EventOneHandler : IDomainEventHandler<EventOne>
        {
            public Task<Result> HandleAsync(EventOne domainEvent, IEventContext context = null)
            {
                if (domainEvent.Data.Data.ContainsKey((Guid)domainEvent.AggregateId))
                {
                    domainEvent.Data.Data[(Guid)domainEvent.AggregateId] += "1";
                }
                else
                {
                    domainEvent.Data.Data.Add((Guid)domainEvent.AggregateId, "1");
                }
                return Task.FromResult(Result.Ok());
            }
        }
        private class EventTwoHandler : IDomainEventHandler<EventTwo>
        {
            public async Task<Result> HandleAsync(EventTwo domainEvent, IEventContext context = null)
            {
                await Task.Delay(10);
                if (domainEvent.Data.Data.ContainsKey((Guid)domainEvent.AggregateId))
                {
                    domainEvent.Data.Data[(Guid)domainEvent.AggregateId] += "2";
                }
                else
                {
                    domainEvent.Data.Data.Add((Guid)domainEvent.AggregateId, "2");
                }
                return Result.Ok();
            }
        }
        private class EventThreeHandler : IDomainEventHandler<EventThree>
        {
            public Task<Result> HandleAsync(EventThree domainEvent, IEventContext context = null)
            {
                if (domainEvent.Data.Data.ContainsKey((Guid)domainEvent.AggregateId))
                {
                    domainEvent.Data.Data[(Guid)domainEvent.AggregateId] += "3";
                }
                else
                {
                    domainEvent.Data.Data.Add((Guid)domainEvent.AggregateId, "3");
                }
                return Task.FromResult(Result.Ok());
            }
        }

        [Fact]
        public async Task PublishEventRange_NoSequence_Should_Respect_Order_SameAggregateId()
        {
            var aggId = Guid.NewGuid();
            var data = new DataHolder();
            var events = new Queue<IDomainEvent>();
            events.Enqueue(new EventOne(data, aggId));
            events.Enqueue(new EventTwo(data, aggId));
            events.Enqueue(new EventThree(data, aggId));

            var builder = new DispatcherConfigurationBuilder();
            builder
                .ForAllEvents()
                .UseBus<InMemoryEventBus>();
            var dispatcher = new BaseDispatcher(builder.Build());

            await dispatcher.PublishEventsRangeAsync(events);
            data.Data[aggId].Should().Be("123");
        }

        [Fact]
        public async Task PublishEventRange_WithSequence_Should_Respect_Order_SameAggregateId()
        {
            var aggId = Guid.NewGuid();
            var aggId2 = Guid.NewGuid();
            var data = new DataHolder();
            var events = new Queue<IDomainEvent>();
            events.Enqueue(new EventOne(data, aggId) { Sequence = 1 });
            events.Enqueue(new EventOne(data, aggId2) { Sequence = 1 });
            events.Enqueue(new EventTwo(data, aggId) { Sequence = 2 });
            events.Enqueue(new EventTwo(data, aggId2) { Sequence = 1 });
            events.Enqueue(new EventThree(data, aggId) { Sequence = 3 });

            var builder = new DispatcherConfigurationBuilder();
            builder
                .ForAllEvents()
                .UseBus<InMemoryEventBus>();
            var dispatcher = new BaseDispatcher(builder.Build());

            await dispatcher.PublishEventsRangeAsync(events);
            data.Data[aggId].Should().Be("123");
            data.Data[aggId2].Should().Be("12");
        }

        #endregion

        #region PublishCommandAsync

        private class TestCommand : ICommand { }

        [Fact]
        public async Task BaseDispatcher_PublishCommandAsync_Should_Not_Throw_Exception_If_NoConfiguration_IsDefined_And_LogWarning()
        {
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns(loggerMock.Object);
            var fakeScopeFactory = new TestScopeFactory(new TestScope(new Dictionary<Type, object>
            {
                {typeof(ILoggerFactory), loggerFactoryMock.Object }
            }));

            bool coreDispatcherCalledWithoutSecurityCritical = false;
            var d = new BaseDispatcher(new CQELight.Dispatcher.Configuration.DispatcherConfiguration(false), fakeScopeFactory);

            var command = new TestCommand();
            CoreDispatcher.OnCommandDispatched += (c) =>
            {
                coreDispatcherCalledWithoutSecurityCritical = object.ReferenceEquals(c, command);
                return Task.FromResult(Result.Ok());
            };

            //Shouldn't throw exception
            await d.DispatchCommandAsync(command).ConfigureAwait(false);

            coreDispatcherCalledWithoutSecurityCritical.Should().BeTrue();
        }

        private class FakeOkResultBus : ICommandBus
        {
            public Task<Result> DispatchAsync(ICommand command, ICommandContext context = null)
            {
                return Task.FromResult(Result.Ok());
            }
        }

        private class FakeFailResultBus : ICommandBus
        {
            public Task<Result> DispatchAsync(ICommand command, ICommandContext context = null)
            {
                return Task.FromResult(Result.Fail());
            }
        }


        [Fact]
        public async Task BaseDispatcher_PublishCommandAsync_WithResult_Should_Enter_InGoodPath_DependingOfResult()
        {
            var cfgBuilder = new DispatcherConfigurationBuilder();
            cfgBuilder
                    .ForCommand<TestCommand>()
                    .UseBuses(typeof(FakeFailResultBus), typeof(FakeOkResultBus));

            var dispatcher = new BaseDispatcher(cfgBuilder.Build());

            bool successCalled = false;
            bool failureCalled = false;

            (await dispatcher.DispatchCommandAsync(new TestCommand()))
                .OnSuccess(() => successCalled = true)
                .OnFailure(() => failureCalled = true);

            successCalled.Should().BeFalse();
            failureCalled.Should().BeTrue();

        }

        private class TestResultCommand : ICommand { }
        private class FakeResultDataBus : ICommandBus
        {
            public Task<Result> DispatchAsync(ICommand command, ICommandContext context = null)
            {
                return Result.Ok("data");
            }
        }

        [Fact]
        public async Task BaseDispatcher_PublishCommandAsync_Should_Keep_Result_Info()
        {
            var cfgBuilder = new DispatcherConfigurationBuilder();
            cfgBuilder
                    .ForCommand<TestResultCommand>()
                    .UseBuses(typeof(FakeResultDataBus));

            var dispatcher = new BaseDispatcher(cfgBuilder.Build());
            
            var result = await dispatcher.DispatchCommandAsync(new TestResultCommand());

            result.Should().BeOfType<Result<string>>();
        }

        #endregion

    }
}
