using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.TestFramework.Extensions;
using FluentAssertions;
using System;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CQELight.TestFramework.Integration.Tests.Extensions
{
    public class AggregateExtensionsTests : BaseUnitTestClass
    {
        #region ClearDomainEvents

        private class TestEvent : BaseDomainEvent { }

        private class Aggregate : AggregateRoot<Guid>
        {
            public void AddEvent() => AddDomainEvent(new TestEvent());
        }

        [Fact]
        public void ClearDomainEvents_Should_Remove_All_AvailableEvents()
        {
            var a = new Aggregate();
            a.AddEvent();

            a.DomainEvents.Should().HaveCount(1);

            a.ClearDomainEvents();

            a.DomainEvents.Should().BeEmpty();
        }

        #endregion

    }
}
