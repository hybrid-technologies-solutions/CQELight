using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using CQELight.Dispatcher;
using CQELight.IoC;
using CQELight.TestFramework;
using CQELight.TestFramework.IoC;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.Buses.InMemory.Integration.Tests
{
    public class InMemoryCommandBusTests : BaseUnitTestClass
    {
        #region Ctor & members

        private class TestNoCreatableHandler : ICommand { }
        private class TestNotFoundCommand : ICommand { }

        private class TestCommand : ICommand
        {
            public string Data { get; set; }
        }

        private class ReflexionNotCreatableHandler : ICommandHandler<TestNoCreatableHandler>
        {
            private readonly string _data;

            public ReflexionNotCreatableHandler(string data)
            {
                _data = data;
            }
            public Task<Result> HandleAsync(TestNoCreatableHandler command, ICommandContext context = null)
                => Result.Ok();
        }

        private class TestCommandHandler : ICommandHandler<TestCommand>
        {
            public static string HandlerData { get; private set; }
            public static string Origin { get; private set; }
            public static void ResetTestData()
                => HandlerData = string.Empty;

            public TestCommandHandler(string data)
            {
                ResetTestData();
                Origin = "spec_ctor";
            }
            public TestCommandHandler()
            {
                ResetTestData();
                Origin = "reflexion";
            }
            public Task<Result> HandleAsync(TestCommand command, ICommandContext context = null)
            {
                HandlerData = command.Data;
                return Result.Ok();
            }
        }

        private class TestIfCommand : ICommand
        {
            public int Data { get; set; }
        }

        private class TestIfCommandHandler : ICommandHandler<TestIfCommand>
        {
            public static int Data { get; private set; }
            public static void ResetData() => Data = 0;
            public Task<Result> HandleAsync(TestIfCommand command, ICommandContext context = null)
            {
                Data = command.Data;
                return Result.Ok();
            }
        }

        private class TestMultipleHandlerIllicit : ICommand { }
        private class TestMultipleHandlerIllicitHandlerOne : ICommandHandler<TestMultipleHandlerIllicit>
        {
            public Task<Result> HandleAsync(TestMultipleHandlerIllicit command, ICommandContext context = null)
            {
                throw new NotImplementedException();
            }
        }
        private class TestMultipleHandlerIllicitHandlerTwo : ICommandHandler<TestMultipleHandlerIllicit>
        {
            public Task<Result> HandleAsync(TestMultipleHandlerIllicit command, ICommandContext context = null)
            {
                throw new NotImplementedException();
            }
        }
        private class TestMultipleHandlerFromConfig : ICommand { }
        private class TestMultipleHandlerFromConfigHandlerOne : ICommandHandler<TestMultipleHandlerFromConfig>
        {
            public Task<Result> HandleAsync(TestMultipleHandlerFromConfig command, ICommandContext context = null)
                => Result.Ok();
        }
        private class TestMultipleHandlerFromConfigHandlerTwo : ICommandHandler<TestMultipleHandlerFromConfig>
        {
            public Task<Result> HandleAsync(TestMultipleHandlerFromConfig command, ICommandContext context = null)
                => Result.Ok();
        }

        private static readonly List<string> s_Order = new List<string>();
        private class TestMultipleHandlerFromConfigParallel : ICommand { }
        private class TestMultipleHandlerFromConfigParallelHandlerOne : ICommandHandler<TestMultipleHandlerFromConfigParallel>
        {
            public Task<Result> HandleAsync(TestMultipleHandlerFromConfigParallel command, ICommandContext context = null)
            {
                s_Order.Add("One");
                return Result.Ok();
            }
        }
        private class TestMultipleHandlerFromConfigParallelHandlerTwo : ICommandHandler<TestMultipleHandlerFromConfigParallel>
        {
            public Task<Result> HandleAsync(TestMultipleHandlerFromConfigParallel command, ICommandContext context = null)
            {
                s_Order.Add("Two");
                return Result.Ok();
            }
        }
        private class TestMultipleHandlerFromConfigParallelHandlerThree : ICommandHandler<TestMultipleHandlerFromConfigParallel>
        {
            public Task<Result> HandleAsync(TestMultipleHandlerFromConfigParallel command, ICommandContext context = null)
            {
                s_Order.Add("Three");
                return Result.Ok();
            }
        }

        public InMemoryCommandBusTests()
        {
            TestCommandHandler.ResetTestData();
            InMemoryCommandBus.InitHandlersCollection(new string[0]);
        }

        #endregion

        #region DispatchAsync

        [Fact]
        public async Task InMemoryCommandBus_DispatchAsync_HandlerFromIoC()
        {
            var factory = new TestScopeFactory();
            factory.Instances.Add(typeof(ICommandHandler<TestCommand>), new TestCommandHandler("tt"));

            CleanRegistrationInDispatcher();
            var bus = new InMemoryCommandBus(null, factory);

            (await bus.DispatchAsync(new TestCommand { Data = "test_ioc" }).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TestCommandHandler.HandlerData.Should().Be("test_ioc");
            TestCommandHandler.Origin.Should().Be("spec_ctor");
        }

        [Fact]
        public async Task InMemoryCommandBus_DispatchAsync_FromCoreDispatcher()
        {
            CleanRegistrationInDispatcher();
            CoreDispatcher.AddHandlerToDispatcher(new TestCommandHandler("coreDispatcher"));
            var bus = new InMemoryCommandBus();

            (await bus.DispatchAsync(new TestCommand { Data = "test_dispatcher" }).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TestCommandHandler.HandlerData.Should().Be("test_dispatcher");
            TestCommandHandler.Origin.Should().Be("spec_ctor");
        }

        [Fact]
        public async Task InMemoryCommandBus_DispatchAsync_Reflexion()
        {
            CleanRegistrationInDispatcher();
            var bus = new InMemoryCommandBus();

            (await bus.DispatchAsync(new TestCommand { Data = "test_ioc" }).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            TestCommandHandler.HandlerData.Should().Be("test_ioc");
            TestCommandHandler.Origin.Should().Be("reflexion");
        }

        [Fact]
        public async Task InMemoryCommandBus_DispatchAsync_NoHandlerFound()
        {
            var hInvoked = false;
            var c = new InMemoryCommandBusConfigurationBuilder().AddHandlerWhenHandlerIsNotFound((cmd, ctx) => hInvoked = true).Build();
            var bus = new InMemoryCommandBus(c);

            (await bus.DispatchAsync(new TestNotFoundCommand()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();
            hInvoked.Should().BeTrue();
        }

        [Fact]
        public async Task InMemoryCommandBus_DispatchAsync_NoHandlerCanBeCreated()
        {
            var hInvoked = false;
            var c = new InMemoryCommandBusConfigurationBuilder().AddHandlerWhenHandlerIsNotFound((cmd, ctx) => hInvoked = true).Build();
            var bus = new InMemoryCommandBus(c);

            (await bus.DispatchAsync(new TestNoCreatableHandler()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();
            hInvoked.Should().BeTrue();
        }

        [Fact]
        public async Task InMemoryCommandBus_DispatchAsync_MultipleHandlersShould_Returns_ResultFail()
        {
            var result = await new InMemoryCommandBus().DispatchAsync(new TestMultipleHandlerIllicit());
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region Configuration

        [Fact]
        public async Task InMemoryCommandBus_Configuration_DispatchIfClause()
        {
            TestIfCommandHandler.ResetData();
            var cfgBuilder =
                new InMemoryCommandBusConfigurationBuilder()
                .DispatchOnlyIf<TestIfCommand>(e => e.Data > 1);

            var b = new InMemoryCommandBus(cfgBuilder.Build());

            TestIfCommandHandler.Data.Should().Be(0);

            await b.DispatchAsync(new TestIfCommand { Data = 1 }).ConfigureAwait(false);

            TestIfCommandHandler.Data.Should().Be(0);

            await b.DispatchAsync(new TestIfCommand { Data = 10 }).ConfigureAwait(false);

            TestIfCommandHandler.Data.Should().Be(10);
        }

        [Fact]
        public async Task InMemoryCommandBus_Configuration_MultipleHandlers_ConfigurationOk()
        {
            var c = new InMemoryCommandBusConfigurationBuilder()
                .AllowMultipleHandlersFor<TestMultipleHandlerFromConfig>();
            var bus = new InMemoryCommandBus(c.Build());

            (await bus.DispatchAsync(new TestMultipleHandlerFromConfig()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task InMemoryCommandBus_Configuration_MultipleHandlers_ConfigurationOk_ShouldWait()
        {
            s_Order.Clear();
            var c = new InMemoryCommandBusConfigurationBuilder()
                .AllowMultipleHandlersFor<TestMultipleHandlerFromConfigParallel>(true);
            var bus = new InMemoryCommandBus(c.Build());

            (await bus.DispatchAsync(new TestMultipleHandlerFromConfigParallel()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

            s_Order.Should().HaveCount(3);
            s_Order.Should().Contain("One");
            s_Order.Should().Contain("Two");
            s_Order.Should().Contain("Three");
        }

        #endregion

        #region CriticalHandler

        private class CriticalCommand : ICommand { }
        private static string HandlersData = "";

        [HandlerPriority(HandlerPriority.High)]
        private class HighPriorityCriticialCommandHandler : ICommandHandler<CriticalCommand>
        {
            public Task<Result> HandleAsync(CriticalCommand command, ICommandContext context = null)
            {
                HandlersData += "A";
                return Result.Ok();
            }
        }

        [HandlerPriority(HandlerPriority.Low)]
        private class LowPriorityCriticalCommandHandler : ICommandHandler<CriticalCommand>
        {
            public Task<Result> HandleAsync(CriticalCommand command, ICommandContext context = null)
            {
                HandlersData += "B";
                return Result.Ok();
            }
        }

        [CriticalHandler]
        private class NotCriticalCommandHandler : ICommandHandler<CriticalCommand>
        {
            public Task<Result> HandleAsync(CriticalCommand command, ICommandContext context = null)
                => throw new NotImplementedException();
        }

        [Fact]
        public async Task Dispatch_Command_CriticalHandlerThrow_Should_NotCallNextHandlers()
        {
            HandlersData = "";
            var cmd = new CriticalCommand();

            var c = new InMemoryCommandBusConfigurationBuilder().AllowMultipleHandlersFor<CriticalCommand>(true).Build();
            var bus = new InMemoryCommandBus(c);

            await bus.DispatchAsync(cmd);

            HandlersData.Should().Be("A");
        }

        #endregion

        #region Result implementation

        private class TestResultOkCommand : ICommand { }
        private class OkResultCommandHandler : ICommandHandler<TestResultOkCommand>
        {
            public Task<Result> HandleAsync(TestResultOkCommand command, ICommandContext context = null)
            {
                return Result.Ok();
            }
        }

        private class TestResultFailCommand : ICommand { }
        private class FailResultCommandHandler : ICommandHandler<TestResultFailCommand>
        {
            public Task<Result> HandleAsync(TestResultFailCommand command, ICommandContext context = null)
            {
                return Result.Fail();
            }
        }

        [Fact]
        public async Task Dispatch_Command_With_Result_Should_Go_To_Correct_Path()
        {
            var bus = new InMemoryCommandBus();

            var cmd = new TestResultOkCommand();
            var okResult = await bus.DispatchAsync(cmd);
            okResult.IsSuccess.Should().BeTrue();

            var cmd2 = new TestResultFailCommand();
            var failResult = await bus.DispatchAsync(cmd2);
            failResult.IsSuccess.Should().BeFalse();
        }

        private class TestMultilpleCommand : ICommand { }

        private class OkResultMultipleHandler : ICommandHandler<TestMultilpleCommand>
        {
            public Task<Result> HandleAsync(TestMultilpleCommand command, ICommandContext context = null)
                => Result.Ok();
        }
        private class FailResultMultipleHandler : ICommandHandler<TestMultilpleCommand>
        {
            public Task<Result> HandleAsync(TestMultilpleCommand command, ICommandContext context = null)
                => Result.Fail();
        }

        [Fact]
        public async Task Dispatch_Command_OneFail_Should_Returns_Failed_Result()
        {
            var c = new InMemoryCommandBusConfigurationBuilder()
                .AllowMultipleHandlersFor<TestMultilpleCommand>()
                .Build();
            var bus = new InMemoryCommandBus(c);

            var cmd = new TestMultilpleCommand();
            var result = await bus.DispatchAsync(cmd);
            result.IsSuccess.Should().BeFalse();

        }

        private class TestResultData : ICommand { }
        private class TestResultDataHandler : ICommandHandler<TestResultData>
        {
            public Task<Result> HandleAsync(TestResultData command, ICommandContext context = null) => Result.Ok("test");
        }

        [Fact]
        public async Task Dispatch_Command_SpecificResult_Should_Not_Loose_Value()
        {
            var bus = new InMemoryCommandBus();

            var cmd = new TestResultData();
            var result = await bus.DispatchAsync(cmd);

            result.Should().BeOfType<Result<string>>();
        }


        #endregion

        #region AutoLoad

        private class AutoLoadCommand : ICommand { }
        private class AutoLoadCommandHandler : ICommandHandler<AutoLoadCommand>
        {
            public static bool Called = false;
            public Task<Result> HandleAsync(AutoLoadCommand command, ICommandContext context = null)
            {
                Called = true;
                return Result.Ok();
            }
        }

        [Fact]
        public async Task AutoLoad_Should_Enable_Bus_WithDefaultConfig()
        {
            AutoLoadCommandHandler.Called = false;
            try
            {
                new Bootstrapper(new BootstrapperOptions { AutoLoad = true }).Bootstrapp();

                await CoreDispatcher.DispatchCommandAsync(new AutoLoadCommand());

                AutoLoadCommandHandler.Called.Should().BeTrue();

            }
            finally
            {
                AutoLoadCommandHandler.Called = false;
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
            using (var scope = DIManager.BeginScope())
            {
                var bus = scope.Resolve<InMemoryEventBus>();
            }
        }

        #endregion

    }
}
