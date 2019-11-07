using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.IoC
{
    /// <summary>
    /// Generic type registration.
    /// </summary>
    /// <typeparam name="T">Type of class to register.</typeparam>
    public class TypeRegistration<T> : TypeRegistration
        where T : class
    {

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="registrationTypes">Registration types.</param>
        public TypeRegistration(params Type[] registrationTypes)
            : base(typeof(T), registrationTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="registrationTypes">Registration types.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        public TypeRegistration(RegistrationLifetime lifetime, params Type[] registrationTypes)
            : base(typeof(T), lifetime, registrationTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="registrationTypes">Registration types.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        /// <param name="mode">Mode to use when searching for ctors.</param>
        public TypeRegistration(RegistrationLifetime lifetime, TypeResolutionMode mode, params Type[] registrationTypes)
            : base(typeof(T), lifetime, mode, registrationTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        public TypeRegistration(bool forEverything) :
            base(typeof(T), forEverything)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        public TypeRegistration(bool forEverything, RegistrationLifetime lifetime) :
            base(typeof(T), forEverything, lifetime)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistration"/> class.
        /// </summary>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        /// <param name="lifetime">Lifetime to consider for this type registration.</param>
        /// <param name="mode">Mode to use when searching for ctors.</param>
        public TypeRegistration(bool forEverything, RegistrationLifetime lifetime, TypeResolutionMode mode) :
            base(typeof(T), forEverything, lifetime, mode)
        {
        }

        #endregion

    }
}
