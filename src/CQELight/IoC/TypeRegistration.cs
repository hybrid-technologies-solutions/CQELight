using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.IoC
{
    /// <summary>
    /// Type for type registration.
    /// </summary>
    public class TypeRegistration : ITypeRegistration
    {
        #region Members

        private readonly List<Type> _abstractionTypes = new List<Type>();

        #endregion

        #region Properties

        /// <summary>
        /// Type registrations.
        /// </summary>
        public IEnumerable<Type> AbstractionTypes => _abstractionTypes.AsEnumerable();

        /// <summary>
        /// Instance type.
        /// </summary>
        public Type InstanceType { get; private set; }

        /// <summary>
        /// Lifetime to use for this registration.
        /// </summary>
        public RegistrationLifetime Lifetime { get; private set; }

        /// <summary>
        /// Resolution mode to use when searching for ctors.
        /// </summary>
        public TypeResolutionMode Mode { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="registrationTypes">Registration types.</param>
        public TypeRegistration(Type instanceType, params Type[] registrationTypes)
            : this(instanceType, RegistrationLifetime.Transient, registrationTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        /// <param name="registrationTypes">Registration types.</param>
        public TypeRegistration(Type instanceType, RegistrationLifetime lifetime, params Type[] registrationTypes)
            : this(instanceType, lifetime, TypeResolutionMode.Full, registrationTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        /// <param name="mode">Mode to use when searching for ctors.</param>
        /// <param name="registrationTypes">Registration types.</param>
        public TypeRegistration(Type instanceType, RegistrationLifetime lifetime, TypeResolutionMode mode, params Type[] registrationTypes)
        {
            InstanceType = instanceType ?? throw new ArgumentNullException(nameof(instanceType));
            _abstractionTypes = registrationTypes?.ToList() ?? new List<Type>();
            _abstractionTypes.Add(instanceType);
            Lifetime = lifetime;
            Mode = mode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        public TypeRegistration(Type instanceType, bool forEverything)
            : this(instanceType, forEverything, RegistrationLifetime.Transient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        public TypeRegistration(Type instanceType, bool forEverything, RegistrationLifetime lifetime)
            :this(instanceType, forEverything, lifetime, TypeResolutionMode.Full)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        /// <param name="mode">Mode to use when searching for ctors.</param>
        public TypeRegistration(Type instanceType, bool forEverything, RegistrationLifetime lifetime, TypeResolutionMode mode)
        {
            InstanceType = instanceType;
            if (forEverything)
            {
                _abstractionTypes.Add(instanceType);
                _abstractionTypes.AddRange(instanceType.GetInterfaces());
            }
            Lifetime = lifetime;
            Mode = mode;
        }


        #endregion

    }
}
