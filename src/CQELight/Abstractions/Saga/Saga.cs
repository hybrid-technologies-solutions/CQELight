using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.Saga.Interfaces;
using CQELight.Dispatcher;
using System;
using System.Threading.Tasks;

namespace CQELight.Abstractions.Saga
{
    /// <summary>
    /// Base class for Saga implementation.
    /// </summary>
    /// <typeparam name="TData">Type of data the saga carries during its life.</typeparam>
    public abstract class Saga<TData> : ISaga
        where TData : class, ISagaData
    {
        #region Members

        private readonly IDispatcher? _dispatcher;

        #endregion

        #region Properties

        /// <summary>
        /// Saga's Id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Saga's data.
        /// </summary>
        public TData? Data { get; protected set; }

        /// <summary>
        /// Completion indicator.
        /// </summary>
        public bool Completed { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new Saga.
        /// </summary>
        protected Saga()
        {
            Id = Guid.NewGuid();
            CoreDispatcher.AddHandlerToDispatcher(this);
        }
        /// <summary>
        /// Creates a new Saga with some initial data.
        /// </summary>
        /// <param name="data">Initial data.</param>
        protected Saga(TData data)
            : this()
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
        /// <summary>
        /// Creates a new Saga with some initial data and a custom dispatcher.
        /// </summary>
        /// <param name="data">Initial data.</param>
        /// <param name="dispatcher">Custom dispatcher.</param>
        protected Saga(TData data, IDispatcher dispatcher)
            : this(data)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Helper method to dispatch a command through Saga custom dispatcher or, if not defined, CoreDispatcher.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        protected Task DispatchCommandAsync(ICommand command)
        {
            if (_dispatcher != null)
            {
                return _dispatcher.DispatchCommandAsync(command, this);
            }
            else
            {
                return CoreDispatcher.DispatchCommandAsync(command, this);
            }
        }

        /// <summary>
        /// Helper method to publish an event through Saga custom dispatcher or, if not defined, CoreDispatcher.
        /// </summary>
        /// <param name="event">Command to dispatch.</param>
        protected Task DispatchEventAsync(IDomainEvent @event)
        {
            if (_dispatcher != null)
            {
                return _dispatcher.PublishEventAsync(@event, this);
            }
            else
            {
                return CoreDispatcher.PublishEventAsync(@event, this);
            }
        }

        /// <summary>
        /// Marks the saga as complete.
        /// </summary>
        protected virtual void MarkAsComplete()
        {
            Completed = true;
            CoreDispatcher.RemoveHandlerFromDispatcher(this);
            DispatchEventAsync(ToSagaFinishedEvent(this));
        }

        /// <summary>
        /// Helper method to generate a SagaFinishedEvent with the saga instance.
        /// </summary>
        /// <param name="saga">Instance of the saga.</param>
        /// <returns>New event.</returns>
        /// <typeparam name="T">Type of saga</typeparam>
        protected IDomainEvent ToSagaFinishedEvent<T>(T saga)
            where T : Saga<TData>
            => new SagaFinishedEvent<T>(saga);

        #endregion

        #region Overriden methods

        /// <summary>
        /// Checks if two sagas are equals, by comparing their id.
        /// </summary>
        /// <param name="obj">Other saga to compare to.</param>
        /// <returns>True if equals, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is Saga<TData> other)
            {
                return other.Id == Id;
            }
            return false;
        }

        /// <summary>
        /// Retrieves the saga hashcode.
        /// </summary>
        /// <returns>Saga's id hashcode</returns>
        public override int GetHashCode()
            => Id.GetHashCode();

        #endregion

        #region Public methods

        /// <summary>
        /// Start asynchronously the saga.
        /// </summary>
        public abstract Task ExecuteSagaAsync();

        #endregion

    }
}
