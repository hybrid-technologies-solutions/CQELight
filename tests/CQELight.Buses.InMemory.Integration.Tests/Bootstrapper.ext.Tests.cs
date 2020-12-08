using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Bootstrapping.Notifications;
using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using CQELight.TestFramework;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CQELight.Buses.InMemory.Integration.Tests
{
    public class BootstrapperExtensionTests : BaseUnitTestClass
    {

        #region Ctor & members

        #endregion

        #region UseInMemoryCommandBus

        [Fact]
        public void UseInMemoryCommandBus_When_Bootstrapping_Should_Not_CreateNotifications_If_NotStrict_And_NotOptimal()
        {
            var b = new Bootstrapper();
            var notifs = b.UseInMemoryCommandBus().Bootstrapp();

            notifs.Should().BeEmpty();
        }

        private class MultipleHandlerCommand : ICommand { }
        private class FirstHandler : ICommandHandler<MultipleHandlerCommand>
        {
            public Task<Result> HandleAsync(MultipleHandlerCommand command, ICommandContext context = null)
                => Task.FromResult(Result.Ok());
        }
        private class SecondHandler : ICommandHandler<MultipleHandlerCommand>
        {
            public Task<Result> HandleAsync(MultipleHandlerCommand command, ICommandContext context = null)
                => Task.FromResult(Result.Ok());
        }

        [Fact]
        public void UseInMemoryCommandBus_Should_Add_Warning_Notification_If_More_Than_One_Handler_Are_Registered_When_Strict_IsPassed()
        {
            var b = new Bootstrapper(true);
            var notifs = b.UseInMemoryCommandBus().Bootstrapp();

            notifs.Should().HaveCountGreaterOrEqualTo(1);

            var notif = notifs.First(e => e.BootstrapperServiceType == typeof(InMemoryCommandBus));
            notif.Type.Should().Be(BootstrapperNotificationType.Warning);
            notif.BootstrapperServiceType.Should().Be(typeof(InMemoryCommandBus));
            notif.ContentType.Should().Be(BootstapperNotificationContentType.CustomServiceNotification);
        }

        private class AloneCommand : ICommand { }

        [Fact]
        public void UseInMemoryCommandBus_Should_Add_Warning_Notification_If_No_Handlers_Exists_When_Optimal_IsPassed()
        {
            var b = new Bootstrapper(false, true);
            var notifs = b.UseInMemoryCommandBus().Bootstrapp();

            notifs.Should().HaveCountGreaterOrEqualTo(1);

            var notif = notifs.First(e => e.BootstrapperServiceType == typeof(InMemoryCommandBus));
            notif.Type.Should().Be(BootstrapperNotificationType.Warning);
            notif.BootstrapperServiceType.Should().Be(typeof(InMemoryCommandBus));
            notif.ContentType.Should().Be(BootstapperNotificationContentType.CustomServiceNotification);
        }

        [Fact]
        public void UseInMemoryCommandBus_Should_Add_Error_Notification_If_More_Than_One_Handler_Are_Registered_When_Strict_And_Optimal_ArePassed()
        {
            var b = new Bootstrapper(true, true);
            var notifs = b.UseInMemoryCommandBus().Bootstrapp();

            notifs.Should().HaveCountGreaterOrEqualTo(1);

            var notif = notifs.First(e => e.BootstrapperServiceType == typeof(InMemoryCommandBus) && e.Type == BootstrapperNotificationType.Error);
            notif.Type.Should().Be(BootstrapperNotificationType.Error);
            notif.BootstrapperServiceType.Should().Be(typeof(InMemoryCommandBus));
            notif.ContentType.Should().Be(BootstapperNotificationContentType.CustomServiceNotification);
        }

        private class MultipleHandlerCriticalCommand : ICommand { }
        private class FirstMutlipleHandler : ICommandHandler<MultipleHandlerCriticalCommand>
        {
            public Task<Result> HandleAsync(MultipleHandlerCriticalCommand command, ICommandContext context = null)
                => Task.FromResult(Result.Ok());
        }
        [CriticalHandler]
        private class SecondMultipleHandler : ICommandHandler<MultipleHandlerCriticalCommand>
        {
            public Task<Result> HandleAsync(MultipleHandlerCriticalCommand command, ICommandContext context = null)
                => Task.FromResult(Result.Ok());
        }

        [Fact]
        public void UseInMemoryCommandBus_Should_Add_Warning_Notification_If_More_Than_One_Handler_Are_Registered_And_One_IsCritical_And_ShouldWait_NotDefined()
        {
            var b = new Bootstrapper();
            var notifs = b.UseInMemoryCommandBus(new InMemoryCommandBusConfigurationBuilder().AllowMultipleHandlersFor<MultipleHandlerCriticalCommand>().Build()).Bootstrapp();

            notifs.Should().HaveCountGreaterOrEqualTo(1);

            var notif = notifs.First(e => e.BootstrapperServiceType == typeof(InMemoryCommandBus) && e.Type == BootstrapperNotificationType.Warning);
            notif.Type.Should().Be(BootstrapperNotificationType.Warning);
            notif.BootstrapperServiceType.Should().Be(typeof(InMemoryCommandBus));
            notif.ContentType.Should().Be(BootstapperNotificationContentType.CustomServiceNotification);
        }

        #endregion

        #region UseInMemoryEventBus

        [Fact]
        public void UseInMemoryEventBus_When_Bootstrapping_Should_Not_CreateNotifications_If_NotStrict_And_NotOptimal()
        {
            var b = new Bootstrapper();
            var notifs = b.UseInMemoryEventBus().Bootstrapp();

            notifs.Should().BeEmpty();
        }

        private class AloneEvent : BaseDomainEvent { }

        [Fact]
        public void UseInMemoryEventBus_Should_Add_Warning_Notification_If_No_Handlers_Exists_When_Optimal_IsPassed()
        {
            var b = new Bootstrapper(false, true);
            var notifs = b.UseInMemoryEventBus().Bootstrapp();

            notifs.Should().HaveCountGreaterOrEqualTo(1);

            var notif = notifs.First(e => e.BootstrapperServiceType == typeof(InMemoryEventBus));
            notif.Type.Should().Be(BootstrapperNotificationType.Warning);
            notif.BootstrapperServiceType.Should().Be(typeof(InMemoryEventBus));
            notif.ContentType.Should().Be(BootstapperNotificationContentType.CustomServiceNotification);
        }

        private class ParallelCriticalEvent : BaseDomainEvent { }
        private class FirstParallelHandler : IDomainEventHandler<ParallelCriticalEvent>
        {
            public Task<Result> HandleAsync(ParallelCriticalEvent domainEvent, IEventContext context = null)
                => Task.FromResult(Result.Ok());
        }
        [CriticalHandler]
        private class SecondParallelHandler : IDomainEventHandler<ParallelCriticalEvent>
        {
            public Task<Result> HandleAsync(ParallelCriticalEvent domainEvent, IEventContext context = null)
                => Task.FromResult(Result.Ok());
        }

        [Fact]
        public void UseInMemoryEventBus_Should_Add_Warning_Notification_If_More_Than_One_Handler_Are_Registered_And_One_IsCritical_And_ParallelHandling_IsDefined()
        {
            var b = new Bootstrapper();
            var notifs = b.UseInMemoryEventBus(new InMemoryEventBusConfigurationBuilder().AllowParallelHandlingFor<ParallelCriticalEvent>().Build()).Bootstrapp();

            notifs.Should().HaveCountGreaterOrEqualTo(1);

            var notif = notifs.First(e => e.BootstrapperServiceType == typeof(InMemoryEventBus) && e.Type == BootstrapperNotificationType.Warning);
            notif.Type.Should().Be(BootstrapperNotificationType.Warning);
            notif.BootstrapperServiceType.Should().Be(typeof(InMemoryEventBus));
            notif.ContentType.Should().Be(BootstapperNotificationContentType.CustomServiceNotification);
        }

        #endregion
    }
}
