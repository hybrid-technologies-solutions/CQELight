using Autofac;
using CQELight.Abstractions.Dispatcher;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Dispatcher;
using CQELight.TestFramework;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.MVVM.Integration.Tests
{
    public class BaseViewModelTests : BaseUnitTestClass
    {
        #region Ctor & members

        private class TestMessage : IMessage
        {

        }

        private class TestViewModel : BaseViewModel, IMessageHandler<TestMessage>
        {
            public static bool IsHandled;

            public static void ResetFlag() => IsHandled = false;

            public TestViewModel()
            {
                ResetFlag();
            }

            public Task HandleMessageAsync(TestMessage message)
            {
                IsHandled = true;
                return Task.CompletedTask;
            }
        }

        private ContainerBuilder GetBasicBuilder()
        {
            var builder = new ContainerBuilder();
            builder.Register(c => new LoggerFactory()).AsImplementedInterfaces();
            return builder;
        }

        public BaseViewModelTests()
        {
            new Bootstrapper().UseAutofacAsIoC(GetBasicBuilder());
        }

        #endregion

        #region HandleMessageAsync

        [Fact]
        public async Task BaseViewModel_Should_BeAble_To_HandleMessage_AfterCtor()
        {
            CleanRegistrationInDispatcher();
            var vm = new TestViewModel();
            var message = new TestMessage();

            TestViewModel.IsHandled.Should().BeFalse();

            await CoreDispatcher.DispatchMessageAsync(message).ConfigureAwait(false);

            TestViewModel.IsHandled.Should().BeTrue();

            vm = null;
        }

        #endregion

    }
}
