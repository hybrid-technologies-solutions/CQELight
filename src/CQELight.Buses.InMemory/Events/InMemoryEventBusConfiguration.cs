using CQELight.Abstractions.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.Buses.InMemory.Events
{
    /// <summary>
    /// Configuration data for InMemory bus
    /// </summary>
    public class InMemoryEventBusConfiguration
    {
        #region Static properties

        /// <summary>
        /// Default configuration.
        /// </summary>
        public static InMemoryEventBusConfiguration Default
            => new InMemoryEventBusConfiguration(3, 500, null);

        #endregion

        #region Members

        internal Dictionary<Type, Func<IDomainEvent, bool>> _ifClauses
           = new Dictionary<Type, Func<IDomainEvent, bool>>();

        internal List<Type> _parallelHandling
            = new List<Type>();

        internal List<Type> _parallelDispatch
            = new List<Type>();

        #endregion

        #region Properties

        /// <summary>
        /// Waiting time between every try.
        /// </summary>
        public ulong WaitingTimeMilliseconds { get; internal set; }
        /// <summary>
        /// Number of retries.
        /// </summary>
        public byte NbRetries { get; internal set; }
        /// <summary>
        /// Callback to invoke when delivery failed.
        /// </summary>
        public Action<IDomainEvent, IEventContext?>? OnFailedDelivery { get; internal set; }
        /// <summary>
        /// Expression used to defined custom if clauses.
        /// </summary>
        public IEnumerable<KeyValuePair<Type, Func<IDomainEvent, bool>>> IfClauses
             => _ifClauses.AsEnumerable();
        /// <summary>
        /// Collection of types that allow parallel handling (meaning that handlers are invoked in parallel).
        /// </summary>
        public IEnumerable<Type> ParallelHandling
             => _parallelHandling.AsEnumerable();
        /// <summary>
        /// Collection of event's types that allow parallel dispatch (meaning that when dispatching a collection of same events of this type, they're dispatch in parallel).
        /// </summary>
        public IEnumerable<Type> ParallelDispatch
            => _parallelDispatch.AsEnumerable();

        #endregion

        #region Ctor

        internal InMemoryEventBusConfiguration()
        {
        }

        private InMemoryEventBusConfiguration(byte nbRetries, ulong waitingTimeMilliseconds,
            Action<IDomainEvent, IEventContext?>? onFailedDelivery)
            : this()
        {
            WaitingTimeMilliseconds = waitingTimeMilliseconds;
            NbRetries = nbRetries;
            OnFailedDelivery = onFailedDelivery;
        }

        #endregion

    }
}
