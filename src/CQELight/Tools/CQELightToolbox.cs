using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;

namespace CQELight.Tools
{
    /// <summary>
    /// A bunch of tools related to CQELight.
    /// </summary>
    public class CQELightToolbox
    {
        #region Properties

        /// <summary>
        /// A factory for creating IoC scopes.
        /// </summary>
        public IScopeFactory ScopeFactory { get; }

        /// <summary>
        /// An instance of the current system dispatcher.
        /// </summary>
        public IDispatcher? Dispatcher { get; }

        /// <summary>
        /// Event store client to access event store data.
        /// </summary>
        public IEventStore? EventStore { get; }

        /// <summary>
        /// Event store client used to access event store from an aggregate perspective.
        /// </summary>
        public IAggregateEventStore? AggregateEventStore { get; }

        #endregion

        #region Ctor

        internal CQELightToolbox(IScopeFactory scopeFactory,
                               IDispatcher? dispatcher = null,
                               IEventStore? eventStore = null,
                               IAggregateEventStore? aggregateEventStore = null)
        {
            ScopeFactory = scopeFactory;
            Dispatcher = dispatcher;
            EventStore = eventStore;
            AggregateEventStore = aggregateEventStore;
        }

        #endregion

    }
}
