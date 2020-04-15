using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;

namespace CQELight.IoC
{
    /// <summary>
    /// Class to hold factory registration.
    /// </summary>
    public class FactoryRegistration : ITypeRegistration
    {
        #region Properties

        /// <summary>
        /// Factory to resolve object that is registered.
        /// </summary>
        public Func<object>? Factory { get; private set; }

        /// <summary>
        /// Scoped factory to resolve object.
        /// </summary>
        public Func<IScope, object>? ScopedFactory { get; private set; }

        /// <summary>
        /// Type to register as.
        /// </summary>
        public IEnumerable<Type> AbstractionTypes { get; private set; }

        /// <summary>
        /// Lifetime to use for this registration.
        /// </summary>
        public RegistrationLifetime Lifetime { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryRegistration"/> class.
        /// </summary>
        /// <param name="factory">Object instance value to register.</param>
        /// <param name="types">Collection of types to register as.</param>
        public FactoryRegistration(Func<object> factory, params Type[] types)
            : this(factory, RegistrationLifetime.Transient, types)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryRegistration"/> class.
        /// </summary>
        /// <param name="scopedFactory">Factory to invoke, with a provided scope, to retrieve instance.</param>
        /// <param name="types">Collection of types to register as</param>
        public FactoryRegistration(Func<IScope, object> scopedFactory, params Type[] types)
            :this(scopedFactory, RegistrationLifetime.Transient, types) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryRegistration"/> class.
        /// </summary>
        /// <param name="scopedFactory">Factory to invoke, with a provided scope, to retrieve instance.</param>
        /// <param name="types">Collection of types to register as</param>
        /// <param name="lifetime">Lifetime to consider for this registration.</param>
        public FactoryRegistration(Func<IScope, object> scopedFactory, RegistrationLifetime lifetime, params Type[] types)
        {
            AbstractionTypes = types ?? throw new ArgumentNullException(nameof(types));
            ScopedFactory = scopedFactory ?? throw new ArgumentNullException(nameof(scopedFactory));
            if (types.Length == 0)
            {
                throw new ArgumentException("FactoryRegistration.ctor() : It's necessary to add at least one type to register as.");
            }
            Lifetime = lifetime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryRegistration"/> class.
        /// </summary>
        /// <param name="factory">Object instance value to register.</param>
        /// <param name="types">Collection of types to register as.</param>
        /// <param name="lifetime">Lifetime to consider for this registration.</param>
        public FactoryRegistration(Func<object> factory, RegistrationLifetime lifetime, params Type[] types)
        {
            AbstractionTypes = types ?? throw new ArgumentNullException(nameof(types));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            if (types.Length == 0)
            {
                throw new ArgumentException("FactoryRegistration.ctor() : It's necessary to add at least one type to register as.");
            }
            Lifetime = lifetime;
        }

        #endregion

    }
}
