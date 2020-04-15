using CQELight.Abstractions.Dispatcher.Configuration.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.Dispatcher.Configuration.Events
{
    /// <summary>
    /// Create a configuration for a single event type.
    /// </summary>
    public class SingleEventTypeConfiguration : IEventConfiguration, IEventDispatcherConfiguration
    {
        #region Members

        internal IList<Type> _busConfigs;
        internal Type _eventType;
        internal bool _isSecurityCritical;
        internal Type? _serializerType;
        internal Action<Exception>? _errorHandler;

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="eventType">Type of event to configure.</param>
        public SingleEventTypeConfiguration(Type eventType)
        {
            _busConfigs = new List<Type>();
            _eventType = eventType;
        }

        #endregion

        #region IEventConfiguration methods

        /// <summary>
        /// Set the 'SecurityCritical' flag on this event type to clone it before sending it to
        /// dispatcher custom callbacks.
        /// </summary>
        /// <returns>Current configuration</returns>
        public IEventDispatcherConfiguration IsSecurityCritical()
        {
            _isSecurityCritical = true;
            return this;
        }

        /// <summary>
        /// Indicates a specific bus to use
        /// </summary>
        /// <typeparam name="T">Type of bus to use.</typeparam>
        /// <returns>Current configuration.</returns>
        public IEventDispatcherConfiguration UseBus<T>() where T : class, IDomainEventBus
        {
            _busConfigs = new[] { typeof(T) };
            return this;
        }

        /// <summary>
        /// Indicates to use all buses available within the system.
        /// </summary>
        /// <returns>Current configuration.</returns>
        public IEventDispatcherConfiguration UseAllAvailableBuses()
        {
            _busConfigs = ReflectionTools.GetAllTypes().Where(t => typeof(IDomainEventBus).IsAssignableFrom(t) && t.IsClass)
                .Distinct(new TypeEqualityComparer())
                .ToArray();
            return this;
        }

        /// <summary>
        /// Indicates to uses specified buses passed as parameter.
        /// </summary>
        /// <param name="types">Buses types to use.</param>
        /// <returns>Current configuration.</returns>
        public IEventDispatcherConfiguration UseBuses(params Type[] types)
        {
            _busConfigs = types;
            return this;
        }

        /// <summary>
        /// Create an error handle to fire if any exception happens on dispatch;
        /// </summary>
        /// <param name="handler">Handler to fire if exception..</param>
        /// <returns>Current configuration.</returns>
        public IEventDispatcherConfiguration HandleErrorWith(Action<Exception> handler)
        {
            _errorHandler = handler;
            return this;
        }

        /// <summary>
        /// Specify the serializer for event transport.
        /// </summary>
        /// <typeparam name="T">Type of serializer to use.</typeparam>
        /// <returns>Current configuration.</returns>
        public IEventDispatcherConfiguration SerializeWith<T>() where T : class, IEventSerializer
        {
            _serializerType = typeof(T);
            return this;
        }

        #endregion

    }
}
