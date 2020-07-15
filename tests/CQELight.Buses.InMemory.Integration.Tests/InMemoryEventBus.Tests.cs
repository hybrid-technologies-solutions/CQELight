using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using CQELight.Dispatcher;
using CQELight.IoC;
using CQELight.IoC.Exceptions;
using CQELight.MVVM;
using CQELight.TestFramework;
using CQELight.TestFramework.IoC;
using CQELight.TestFramework.Logging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.Buses.InMemory.Integration.Tests
{
    public class InMemoryEventBus_Tests : BaseUnitTestClass
    {
        public InMemoryEventBus_Tests()
        {
            InMemoryEventBus.InitHandlersCollection(new string[0]);
            CleanRegistrationInDispatcher();
        }

        #region PublishEventAsync

        #region Basic publish

        private class TestEvent : BaseDomainEvent
        {
            public string Data { get; set; }
        }

        private class TestEventContextHandler : IDomainEventHandler<TestEvent>, IEventContext
        {
            public static string Data { get; private set; }
            public static int Dispatcher { get; private set; }
            public static int CallTimes { get; set; }

            public static void ResetData()
            => Data = string.Empty;
            public TestEventContextHandler(int dispatcher)
            {
                ResetData();
                Dispatcher = dispatcher;
            }
            public Task<Result> HandleAsync(TestEvent domainEvent, IEventContext context = null)
            {
                Data = domainEvent.Data;
                CallTimes++;
                return Task.FromResult(Result.Ok());
            }
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_ContextAsHandler()
        {
            var b = new InMemoryEventBus();
            (await b.PublishEventAsync(new TestEvent { Data = "to_ctx" }, new TestEventContextHandler(0)).ConfigureAwait(false))
                .IsSuccess.Should().BeTrue();

            TestEventContextHandler.Data.Should().Be("to_ctx");
            TestEventContextHandler.Dispatcher.Should().Be(0);
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_HandlerInDispatcher()
        {
            CoreDispatcher.AddHandlerToDispatcher(new TestEventContextHandler(1));
            var b = new InMemoryEventBus();
            (await b.PublishEventAsync(new TestEvent { Data = "to_ctx" }).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TestEventContextHandler.Data.Should().Be("to_ctx");
            TestEventContextHandler.Dispatcher.Should().Be(1);
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_HandlerInDispatcher_Multiples_Instances()
        {
            CoreDispatcher.AddHandlerToDispatcher(new TestEventContextHandler(1));
            CoreDispatcher.AddHandlerToDispatcher(new TestEventContextHandler(1));
            CoreDispatcher.AddHandlerToDispatcher(new TestEventContextHandler(1));
            var b = new InMemoryEventBus();
            (await b.PublishEventAsync(new TestEvent { Data = "to_ctx" }).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TestEventContextHandler.Data.Should().Be("to_ctx");
            TestEventContextHandler.Dispatcher.Should().Be(1);
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_HandlerSameInstance_Should_NotBeCalledTwice()
        {
            TestEventContextHandler.CallTimes = 0;
            var h = new TestEventContextHandler(1);
            CoreDispatcher.AddHandlerToDispatcher(new TestEventContextHandler(1));
            CoreDispatcher.AddHandlerToDispatcher(h);
            CoreDispatcher.AddHandlerToDispatcher(h);

            var b = new InMemoryEventBus();
            (await b.PublishEventAsync(new TestEvent { Data = "to_ctx" }).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TestEventContextHandler.Data.Should().Be("to_ctx");
            TestEventContextHandler.Dispatcher.Should().Be(1);
            TestEventContextHandler.CallTimes.Should().Be(1);
        }

        #endregion

        #region Error Management

        private class TestResultFailedEvent : BaseDomainEvent { }
        private class ResultFailedEventHandler : IDomainEventHandler<TestResultFailedEvent>
        {
            public Task<Result> HandleAsync(TestResultFailedEvent domainEvent, IEventContext context = null)
            {
                return Task.FromResult(Result.Fail());
            }
        }
        private class ResultOkEventHandler : IDomainEventHandler<TestResultFailedEvent>
        {
            public Task<Result> HandleAsync(TestResultFailedEvent domainEvent, IEventContext context = null)
            {
                return Task.FromResult(Result.Ok());
            }
        }


        private class TestExceptionEvent : BaseDomainEvent { }
        private class ExceptionHandler : IDomainEventHandler<TestExceptionEvent>
        {
            public Task<Result> HandleAsync(TestExceptionEvent domainEvent, IEventContext context = null)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_ExceptionInHandler()
        {
            var b = new InMemoryEventBus();
            (await b.PublishEventAsync(new TestExceptionEvent(), null).ConfigureAwait(false)).IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task PublishEventAsync_Should_ResultFail_ShouldBeReturned()
        {
            var b = new InMemoryEventBus();
            (await b.PublishEventAsync(new TestResultFailedEvent(), null).ConfigureAwait(false)).IsSuccess.Should().BeFalse();
        }

        #endregion

        #region RetryStrategy

        private class TestRetryEvent : BaseDomainEvent
        {
            public int NbTries { get; set; }
        }
        private class TestRetryEventHandler : IDomainEventHandler<TestRetryEvent>
        {
            public Task<Result> HandleAsync(TestRetryEvent domainEvent, IEventContext context = null)
            {
                if (domainEvent.NbTries != 2)
                {
                    domainEvent.NbTries++;
                    throw new Exception("NbTries == 2");
                }
                else
                {
                    return Task.FromResult(Result.Ok());
                }
            }
        }

        [Fact]
        public async Task InMemoryEventBus_Configuration_RetryStrategy_Callback()
        {
            bool callbackCalled = false;
            var cfgBuilder =
                new InMemoryEventBusConfigurationBuilder()
                .SetRetryStrategy(100, 1)
                .DefineErrorCallback((e, ctx) => callbackCalled = true);

            var b = new InMemoryEventBus(cfgBuilder.Build());
            (await b.PublishEventAsync(new TestRetryEvent()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();

            callbackCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InMemoryEventBus_Configuration_RetryStrategy_CallbackNoInvoked()
        {
            bool callbackCalled = false;
            var cfgBuilder =
                new InMemoryEventBusConfigurationBuilder()
                .SetRetryStrategy(100, 3)
                .DefineErrorCallback((e, ctx) => callbackCalled = true);

            var b = new InMemoryEventBus(cfgBuilder.Build());
            (await b.PublishEventAsync(new TestRetryEvent()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            callbackCalled.Should().BeFalse();
        }

        private class ErrorEvent : BaseDomainEvent
        {
            public string Data { get; set; } = "";
        }
        [HandlerPriority(HandlerPriority.High)]
        private class ErrorHandlerOne : IDomainEventHandler<ErrorEvent>
        {
            public Task<Result> HandleAsync(ErrorEvent domainEvent, IEventContext context = null)
            {
                domainEvent.Data += "1";
                return Task.FromResult(Result.Ok());
            }
        }
        private class ErrorHandlerTwo : IDomainEventHandler<ErrorEvent>
        {
            public Task<Result> HandleAsync(ErrorEvent domainEvent, IEventContext context = null)
            {
                domainEvent.Data += "2";
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_OneHandlerInError_Should_NotDispatch_MultipleTimes_ToSuccessfullHandlers()
        {
            var errorEvent = new ErrorEvent();
            var b = new InMemoryEventBus(new InMemoryEventBusConfigurationBuilder()
                .SetRetryStrategy(1, 2).Build());

            (await b.PublishEventAsync(errorEvent)).IsSuccess.Should().BeFalse();

            errorEvent.Data.Should().Be("1222");
        }

        #endregion

        #region IoC Resolving

        private class UnresolvableEvent : BaseDomainEvent { }
        private class UnresolvableEventHandler : IDomainEventHandler<UnresolvableEvent>
        {
            public UnresolvableEventHandler(object objValue)
            {
                ObjValue = objValue;
            }

            public object ObjValue { get; }

            public Task<Result> HandleAsync(UnresolvableEvent domainEvent, IEventContext context = null)
            {
                return Task.FromResult(Result.Ok());
            }
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_ResolutionException_Should_NotTryToResolveTwice()
        {
            var scopeMock = new Mock<IScope>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            var fakeLogger = new FakeLogger(LogLevel.Error);

            loggerFactoryMock.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(fakeLogger);

            scopeMock.Setup(m => m.Resolve<ILoggerFactory>(It.IsAny<IResolverParameter[]>()))
                .Returns(loggerFactoryMock.Object);

            scopeMock.Setup(m => m.Resolve(It.IsAny<Type>(), It.IsAny<IResolverParameter[]>()))
                .Throws(new IoCResolutionException());

            var b = new InMemoryEventBus(null, new TestScopeFactory(scopeMock.Object));
            (await b.PublishEventAsync(new UnresolvableEvent()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();

            scopeMock.Verify(m => m.Resolve(typeof(UnresolvableEventHandler), It.IsAny<IResolverParameter[]>()), Times.Once());

            (await b.PublishEventAsync(new UnresolvableEvent()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();

            scopeMock.Verify(m => m.Resolve(typeof(UnresolvableEventHandler), It.IsAny<IResolverParameter[]>()), Times.Once());
            fakeLogger.CurrentLogValue.Should().NotBeNullOrEmpty();
            fakeLogger.NbLogs.Should().Be(1);
        }

        public class VMEvent : BaseDomainEvent { }
        public class SubVMEvent : BaseDomainEvent { }
        public class ViewModel : BaseViewModel, IDomainEventHandler<VMEvent>
        {
            public Task<Result> HandleAsync(VMEvent domainEvent, IEventContext context = null)
            {
                throw new NotImplementedException();
            }
        }
        class SubViewModel : ViewModel, IDomainEventHandler<SubVMEvent>
        {
            public Task<Result> HandleAsync(SubVMEvent domainEvent, IEventContext context = null)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task InMemoryEventBus_ResolutionError_Type_IsViewModel_Should_NotLogAnything()
        {
            var scopeMock = new Mock<IScope>();
            var fakeLogger = new FakeLogger(LogLevel.Error);
            var loggerFactoryMock = new Mock<ILoggerFactory>();

            loggerFactoryMock.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(fakeLogger);

            scopeMock.Setup(m => m.Resolve<ILoggerFactory>(It.IsAny<IResolverParameter[]>()))
                .Returns(loggerFactoryMock.Object);

            scopeMock.Setup(m => m.Resolve(typeof(ViewModel), It.IsAny<IResolverParameter[]>())).Throws(new Exception());

            var b = new InMemoryEventBus(null, new TestScopeFactory(scopeMock.Object));
            (await b.PublishEventAsync(new VMEvent()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();

            fakeLogger.CurrentLogValue.Should().BeNullOrEmpty();
            fakeLogger.NbLogs.Should().Be(0);
        }

        [Fact]
        public async Task InMemoryEventBus_ResolutionError_Type_IsSubViewModel_Should_NotLogAnything()
        {
            var scopeMock = new Mock<IScope>();
            var fakeLogger = new FakeLogger(LogLevel.Error);
            var loggerFactoryMock = new Mock<ILoggerFactory>();

            loggerFactoryMock.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(fakeLogger);

            scopeMock.Setup(m => m.Resolve<ILoggerFactory>(It.IsAny<IResolverParameter[]>()))
                .Returns(loggerFactoryMock.Object);

            scopeMock.Setup(m => m.Resolve(typeof(SubViewModel), It.IsAny<IResolverParameter[]>())).Throws(new Exception());

            var b = new InMemoryEventBus(null, new TestScopeFactory(scopeMock.Object));
            (await b.PublishEventAsync(new SubVMEvent()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();

            fakeLogger.CurrentLogValue.Should().BeNullOrEmpty();
            fakeLogger.NbLogs.Should().Be(0);

        }

        #endregion

        #region Priority

        private class OrderedEvent : BaseDomainEvent
        { }

        private static string s_OrderString = "";
        [HandlerPriority(HandlerPriority.High)]
        private class HighPriorityHandler : IDomainEventHandler<OrderedEvent>
        {
            public Task<Result> HandleAsync(OrderedEvent domainEvent, IEventContext context = null)
            {
                s_OrderString += "H";
                return Task.FromResult(Result.Ok());
            }
        }
        private class NormalPriorityHandler : IDomainEventHandler<OrderedEvent>
        {
            public Task<Result> HandleAsync(OrderedEvent domainEvent, IEventContext context = null)
            {
                s_OrderString += "N";
                return Task.FromResult(Result.Ok());
            }
        }
        [HandlerPriority(HandlerPriority.Low)]
        private class LowPriorityHandler : IDomainEventHandler<OrderedEvent>
        {
            public Task<Result> HandleAsync(OrderedEvent domainEvent, IEventContext context = null)
            {
                s_OrderString += "L";
                return Task.FromResult(Result.Ok());
            }
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_Should_Respect_Priority()
        {
            var b = new InMemoryEventBus();

            s_OrderString.Should().BeNullOrWhiteSpace();

            (await b.PublishEventAsync(new OrderedEvent()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            s_OrderString.Should().Be("HNL");
        }

        #endregion

        #region If Dispatch Condition

        private class TestIfEvent : BaseDomainEvent
        {
            public int Data { get; set; }
        }
        private class TestIfEventHandler : IDomainEventHandler<TestIfEvent>
        {
            public static int Data { get; private set; }
            public Task<Result> HandleAsync(TestIfEvent domainEvent, IEventContext context = null)
            {
                Data = domainEvent.Data;
                return Task.FromResult(Result.Ok());
            }

            internal static void ResetData() => Data = 0;
        }

        [Fact]
        public async Task InMemoryEventBus_Configuration_DispatchIfClause()
        {
            TestIfEventHandler.ResetData();
            var cfgBuilder =
                new InMemoryEventBusConfigurationBuilder()
                .DispatchOnlyIf<TestIfEvent>(e => e.Data > 1);

            var b = new InMemoryEventBus(cfgBuilder.Build());

            TestIfEventHandler.Data.Should().Be(0);

            (await b.PublishEventAsync(new TestIfEvent { Data = 1 }).ConfigureAwait(false)).IsSuccess.Should().BeFalse();

            TestIfEventHandler.Data.Should().Be(0);

            (await b.PublishEventAsync(new TestIfEvent { Data = 10 }).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TestIfEventHandler.Data.Should().Be(10);
        }

        #endregion

        #region Parallel dispatch

        private class ParallelEvent : BaseDomainEvent
        {
            public List<int> ThreadsInfos = new List<int>();
            public bool RetryMode = false;
            private readonly SemaphoreSlim sem = new SemaphoreSlim(1);
            public int NbTries { get; set; } = 0;
            public void AddThreadInfos(int i)
            {
                sem.Wait();
                ThreadsInfos.Add(i);
                sem.Release();
            }
        }

        private class ParallelEventHandlerOne : IDomainEventHandler<ParallelEvent>
        {
            public async Task<Result> HandleAsync(ParallelEvent domainEvent, IEventContext context = null)
            {
                await Task.Delay(100).ConfigureAwait(false);
                domainEvent.AddThreadInfos(Thread.CurrentThread.ManagedThreadId);
                return Result.Ok();
            }
        }

        private class ParallelEventHandlerTwo : IDomainEventHandler<ParallelEvent>
        {
            public async Task<Result> HandleAsync(ParallelEvent domainEvent, IEventContext context = null)
            {
                await Task.Delay(150).ConfigureAwait(false);
                if (domainEvent.RetryMode && domainEvent.NbTries < 2)
                {
                    domainEvent.NbTries++;
                    throw new InvalidOperationException();
                }
                domainEvent.AddThreadInfos(Thread.CurrentThread.ManagedThreadId);
                return Result.Ok();
            }
        }

        [Fact]
        public async Task InMemoryEventBus_Configuration_RetryStrategy_WhenDispatchParallel()
        {
            bool callbackCalled = false;
            var cfgBuilder =
                new InMemoryEventBusConfigurationBuilder()
                .SetRetryStrategy(100, 3)
                .DefineErrorCallback((e, ctx) => callbackCalled = true)
                .AllowParallelDispatchFor<ParallelEvent>();

            var b = new InMemoryEventBus(cfgBuilder.Build());

            var evt = new ParallelEvent()
            {
                RetryMode = true
            };
            (await b.PublishEventAsync(evt).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            callbackCalled.Should().BeFalse();
            evt.ThreadsInfos.Should().HaveCount(2);
        }

        [Fact]
        public async Task InMemoryEventBus_Configuration_ParallelDispatch()
        {
            var cfgBuilder =
                new InMemoryEventBusConfigurationBuilder()
                .AllowParallelDispatchFor<ParallelEvent>();

            var b = new InMemoryEventBus(cfgBuilder.Build());

            var evt = new ParallelEvent();
            (await b.PublishEventAsync(evt).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            evt.ThreadsInfos.Should().HaveCount(2);
        }

        #endregion

        #region Critical handlers
        private class CriticalEvent : BaseDomainEvent
        {
            public string HandlerData { get; set; } = "";
            public int NbTries { get; set; } = 0;
        }

        [HandlerPriority(HandlerPriority.High)]
        private class HighPriorityCriticialEventHandler : IDomainEventHandler<CriticalEvent>
        {
            public Task<Result> HandleAsync(CriticalEvent domainEvent, IEventContext context = null)
            {
                domainEvent.HandlerData += "A";
                return Task.FromResult(Result.Ok());
            }
        }

        [HandlerPriority(HandlerPriority.Low)]
        private class LowPriorityCriticalCommandHandler : IDomainEventHandler<CriticalEvent>
        {
            public Task<Result> HandleAsync(CriticalEvent domainEvent, IEventContext context = null)
            {
                domainEvent.HandlerData += "C";
                return Task.FromResult(Result.Ok());
            }
        }

        [CriticalHandler]
        private class CriticalEventHandler : IDomainEventHandler<CriticalEvent>
        {
            public Task<Result> HandleAsync(CriticalEvent domainEvent, IEventContext context = null)
            {
                domainEvent.HandlerData += "B";
                domainEvent.NbTries++;
                if (domainEvent.NbTries < 3)
                {
                    throw new NotImplementedException();
                }
                return Task.FromResult(Result.Ok());
            }
        }

        [Fact]
        public async Task Publish_Event_CriticalHandlerThrow_Should_NotCallNextHandlers()
        {
            var evt = new CriticalEvent();
            var cfgBuilder =
               new InMemoryEventBusConfigurationBuilder();
            var bus = new InMemoryEventBus(cfgBuilder.Build());

            (await bus.PublishEventAsync(evt)).IsSuccess.Should().BeTrue();

            evt.HandlerData.Should().Be("AB");
        }

        [Fact]
        public async Task Publish_Event_CriticalHandlerThrow_Should_BeTheOnlyOneRetried_If_RetryStrategy_IsDefined_And_NotGoForward()
        {
            var evt = new CriticalEvent();
            var cfgBuilder =
               new InMemoryEventBusConfigurationBuilder()
               .SetRetryStrategy(1, 1);
            var bus = new InMemoryEventBus(cfgBuilder.Build());

            (await bus.PublishEventAsync(evt)).IsSuccess.Should().BeFalse();

            evt.HandlerData.Should().Be("ABB");
            evt.NbTries.Should().Be(2);
        }

        [Fact]
        public async Task Publish_Event_CriticalHandlerThrow_Should_BeTheOnlyOneRetried_If_RetryStrategy_IsDefined()
        {
            var evt = new CriticalEvent();
            var cfgBuilder =
               new InMemoryEventBusConfigurationBuilder()
               .SetRetryStrategy(1, 3);
            var bus = new InMemoryEventBus(cfgBuilder.Build());

            (await bus.PublishEventAsync(evt)).IsSuccess.Should().BeTrue();

            evt.HandlerData.Should().Be("ABBBC");
            evt.NbTries.Should().Be(3);
        }

        #endregion

        #region Transanctionnal event

        private class TransactionEvent : BaseTransactionnalEvent
        {
            public TransactionEvent(params IDomainEvent[] events)
                : base(events) { }
        }

        private class Event1 : BaseDomainEvent
        {
            public string Data { get; set; }
        }
        private class Event2 : BaseDomainEvent
        {
            public string Data { get; set; }
        }
        private class Event3 : BaseDomainEvent
        {
            public string Data { get; set; }
        }

        private class TransactionEventHandler : BaseTransactionnalEventHandler<TransactionEvent>
        {
            public static string DataParsed { get; set; }
            public string BeforeData { get; private set; }
            public string AfterData { get; private set; }

            public TransactionEventHandler()
            {
            }

            protected override Task<Result> AfterTreatEventsAsync()
            {
                AfterData = "AFTER";
                return base.AfterTreatEventsAsync();
            }

            protected override Task<Result> BeforeTreatEventsAsync()
            {
                BeforeData = "BEFORE";
                return base.BeforeTreatEventsAsync();
            }

            protected override Task<Result> TreatEventAsync(IDomainEvent evt)
            {
                if (evt is Event1 evt1)
                {
                    DataParsed += "|1:" + evt1.Data;
                }
                else if (evt is Event2 evt2)
                {
                    DataParsed += "|2:" + evt2.Data;
                }
                else if (evt is Event3 evt3)
                {
                    DataParsed += "|3:" + evt3.Data;
                }
                return Task.FromResult(Result.Ok());
            }
        }

        [Fact]
        public async Task InMemoryEventBus_PublishEventAsync_TransactionnalEvent()
        {
            TransactionEventHandler.DataParsed = string.Empty;

            var evt = new TransactionEvent(
             new Event1 { Data = "Data1" },
             new Event2 { Data = "Data2" },
             new Event3 { Data = "Data3" }
             );

            var b = new InMemoryEventBus();
            (await b.PublishEventAsync(evt).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TransactionEventHandler.DataParsed.Should().Be("|1:Data1|2:Data2|3:Data3");
        }

        #endregion

        #endregion

        #region AutoLoad

        private class AutoLoadDomainEvent : BaseDomainEvent { }
        private class AutoLoadDomainEventHandler : IDomainEventHandler<AutoLoadDomainEvent>
        {
            public static bool Called = false;
            public Task<Result> HandleAsync(AutoLoadDomainEvent domainEvent, IEventContext context = null)
            {
                Called = true;
                return Result.Ok();
            }
        }

        [Fact]
        public async Task AutoLoad_Should_Enable_Bus_WithDefaultConfig()
        {
            AutoLoadDomainEventHandler.Called = false;
            try
            {
                new Bootstrapper(new BootstrapperOptions { AutoLoad = true }).Bootstrapp();

                await CoreDispatcher.PublishEventAsync(new AutoLoadDomainEvent());

                AutoLoadDomainEventHandler.Called.Should().BeTrue();

            }
            finally
            {
                AutoLoadDomainEventHandler.Called = false;
            }
        }

        #endregion

        #region IoC

        [Fact]
        public void InMemoryEventBus_Should_Be_Resolvable_By_MicrosoftIoC()
        {
            new Bootstrapper()
                .UseInMemoryCommandBus()
                .UseMicrosoftDependencyInjection(new ServiceCollection())
                .Bootstrapp();
            using(var scope = DIManager.BeginScope())
            {
                var bus = scope.Resolve<InMemoryCommandBus>();
                bus.Should().NotBeNull();
            }
        }

        #endregion

    }
}
