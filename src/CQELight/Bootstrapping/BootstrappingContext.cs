using CQELight.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight
{
    /// <summary>
    /// Bootstrapping context that allows to perform more accurate bootstrapping.
    /// </summary>
    public class BootstrappingContext
    {
        #region Members

        private readonly IEnumerable<BootstrapperServiceType> _registeredServices;
        private readonly IEnumerable<Type> _iocRegisteredTypes;

        #endregion

        #region Properties

        /// <summary>
        /// Flag that indicates if strict mode has been asked to bootstrapper.
        /// In your extensions, strict mode should be used to check for example
        /// that some things haven't been configured uselessly or that best practices are applied by callers.
        /// Generally, strict checks should generates Error notifications.
        /// </summary>
        public bool Strict { get; internal set; }

        /// <summary>
        /// Flag that indicates if checkOptimal has been asked to bootstrapper.
        /// In your extensions, checkOptimal mode should be used for example to perform
        /// some custom checks that ensure that the system will work as optimal as possible.
        /// Generally, checkOptimal checks should generates Warning or Info notifications.
        /// </summary>
        public bool CheckOptimal { get; internal set; }

        /// <summary>
        /// Associated bootstrapper instance.
        /// </summary>
        public Bootstrapper Bootstrapper { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Default ctor.
        /// </summary>
        /// <param name="registeredServices">Collection of registered services.</param>
        /// <param name="iocRegisteredTypes">Collection of types that are registered in Bootstrapper IoC registrations
        /// collection</param>
        /// <param name="bootstrapper">Instance of boostrapper</param>
        internal BootstrappingContext(
            IEnumerable<BootstrapperServiceType> registeredServices,
            IEnumerable<Type> iocRegisteredTypes,
            Bootstrapper bootstrapper)
        {
            _registeredServices = registeredServices;
            _iocRegisteredTypes = iocRegisteredTypes;
            Bootstrapper = bootstrapper;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Allow to check if a specific service has been registered.
        /// </summary>
        /// <param name="type">Type of service to check.</param>
        /// <returns>True if service is registered, false otherwise.</returns>
        public bool IsServiceRegistered(BootstrapperServiceType type)
            => _registeredServices.Any(s => s == type);

        /// <summary>
        /// Check if a type has been registered into Bootstrapper IoC list
        /// at least one. This DOESN'T check custom IoC plugin registrations.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if type as at least one registration, false otherwise.</returns>
        public bool IsAbstractionRegisteredInIoC(Type type)
            => _iocRegisteredTypes?.Any(t => new TypeEqualityComparer().Equals(type, t)) == true;

        #endregion

    }
}
