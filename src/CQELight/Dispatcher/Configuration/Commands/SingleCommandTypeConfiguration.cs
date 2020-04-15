using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Dispatcher.Configuration.Commands.Interfaces;
using CQELight.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.Dispatcher.Configuration.Commands
{
    /// <summary>
    /// Create a configuration for a single command type.
    /// </summary>
    public class SingleCommandTypeConfiguration : ICommandConfiguration, ICommandDispatcherConfiguration
    {
        #region Members

        internal IList<Type> _busConfigs;
        internal Type _commandType;
        internal bool _isSecurityCritical;
        internal Type? _serializerType;
        internal Action<Exception>? _errorHandler;

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="commandType">Type of event to configure.</param>
        public SingleCommandTypeConfiguration(Type commandType)
        {
            _busConfigs = new List<Type>();
            _commandType = commandType;
        }

        #endregion

        #region IEventConfiguration methods

        /// <summary>
        /// Set the 'SecurityCritical' flag on this event type to clone it before sending it to
        /// dispatcher custom callbacks.
        /// </summary>
        /// <returns>Current configuration</returns>
        public ICommandDispatcherConfiguration IsSecurityCritical()
        {
            _isSecurityCritical = true;
            return this;
        }

        /// <summary>
        /// Indicates a specific bus to use
        /// </summary>
        /// <typeparam name="T">Type of bus to use.</typeparam>
        /// <returns>Current configuration.</returns>
        public ICommandDispatcherConfiguration UseBus<T>() where T : class, ICommandBus
        {
            _busConfigs = new[] { typeof(T) };
            return this;
        }

        /// <summary>
        /// Indicates to use all buses available within the system.
        /// </summary>
        /// <returns>Current configuration.</returns>
        public ICommandDispatcherConfiguration UseAllAvailableBuses()
        {
            _busConfigs = ReflectionTools.GetAllTypes().Where(t => typeof(ICommandBus).IsAssignableFrom(t) && t.IsClass)
                .Distinct(new TypeEqualityComparer())
                .ToArray();
            return this;
        }

        /// <summary>
        /// Indicates to uses specified buses passed as parameter.
        /// </summary>
        /// <param name="types">Buses types to use.</param>
        /// <returns>Current configuration.</returns>
        public ICommandDispatcherConfiguration UseBuses(params Type[] types)
        {
            _busConfigs = types;
            return this;
        }

        /// <summary>
        /// Create an error handle to fire if any exception happens on dispatch;
        /// </summary>
        /// <param name="handler">Handler to fire if exception..</param>
        /// <returns>Current configuration.</returns>
        public ICommandDispatcherConfiguration HandleErrorWith(Action<Exception> handler)
        {
            _errorHandler = handler;
            return this;
        }

        /// <summary>
        /// Specify the serializer for command transport.
        /// </summary>
        /// <typeparam name="T">Type of serializer to use.</typeparam>
        /// <returns>Current configuration.</returns>
        public ICommandDispatcherConfiguration SerializeWith<T>() where T : class, ICommandSerializer
        {
            _serializerType = typeof(T);
            return this;
        }

        #endregion

    }
}
