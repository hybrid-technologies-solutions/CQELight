using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQELight.TestFramework.Fakes.Buses
{
    /// <summary>
    /// Fake event bus used to test events.
    /// </summary>
    public class FakeEventBus : IDomainEventBus
    {
        #region Members

        internal List<IDomainEvent> _events = new List<IDomainEvent>();
        private readonly Result? expectedResult;

        #endregion

        #region Properties

        /// <summary>
        /// List of all evens.
        /// </summary>
        public IEnumerable<IDomainEvent> Events => _events.AsEnumerable();

        #endregion

        #region Ctor

        /// <summary>
        /// Creates an new <see cref="FakeEventBus"/> with expected result for publish calls.
        /// </summary>
        /// <param name="expectedResult">Predefined result to returns. If null, ok will be returned</param>
        public FakeEventBus(Result? expectedResult = null)
        {
            this.expectedResult = expectedResult;
        }

        #endregion

        #region IDomainEventBus methods

        public Task<Result> PublishEventAsync(IDomainEvent @event, IEventContext? context = null)
        {
            _events.Add(@event);
            return Task.FromResult(expectedResult ?? Result.Ok());
        }

        public Task<Result> PublishEventRangeAsync(IEnumerable<IDomainEvent> events)
        {
            _events.AddRange(events);
            return Task.FromResult(expectedResult ?? Result.Ok());
        }

        #endregion

    }
}
