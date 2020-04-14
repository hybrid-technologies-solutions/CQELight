using CQELight.Tools;
using CQELight.TestFramework.IoC;
using Moq;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;

namespace CQELight.TestFramework.Extensions
{
    /// <summary>
    /// Extensions upon CQELightToolbox
    /// </summary>
    public static class CQELightToolBoxExtensions
    {
        /// <summary>
        /// Gets a test toolbox, with your configured tooling.
        /// </summary>
        /// <param name="scopeFactory">Scope factory to use. If not defined, and empty one will be provided.</param>
        /// <param name="dispatcherMock">Dispatcher mock to use. If not defined, and empty one will be provided.</param>
        /// <param name="eventStoreMock">EventStore mock to use. If not defined, and empty one will be provided.</param>
        /// <param name="aggregateEventStoreMock">AggregateEventStore mock to use. If not defined, and empty one will be provided.</param>
        /// <returns></returns>
        public static CQELightToolbox GetTestToolbox(
            TestScopeFactory? scopeFactory = null,
            Mock<IDispatcher>? dispatcherMock = null,
            Mock<IEventStore>? eventStoreMock = null,
            Mock<IAggregateEventStore>? aggregateEventStoreMock = null)
            => new CQELightToolbox(
                    scopeFactory ?? new TestScopeFactory(),
                    dispatcherMock?.Object ?? new Mock<IDispatcher>().Object,
                    eventStoreMock?.Object ?? new Mock<IEventStore>().Object,
                    aggregateEventStoreMock?.Object ?? new Mock<IAggregateEventStore>().Object);
    }
}
