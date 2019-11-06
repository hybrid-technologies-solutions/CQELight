using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.IoC
{
    /// <summary>
    /// Class to hold type registration to add to ioc container.
    /// </summary>
    public class InstanceTypeRegistration : ITypeRegistration
    {
        #region Properties

        /// <summary>
        /// Value to register.
        /// </summary>
        public object Value { get; private set; }
        /// <summary>
        /// Type to register as.
        /// </summary>
        public IEnumerable<Type> AbstractionTypes { get; private set; }
        /// <summary>
        /// Lifetime of this registration
        /// </summary>
        public RegistrationLifetime Lifetime { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceTypeRegistration"/> class.
        /// </summary>
        /// <param name="value">Object instance value to register.</param>
        /// <param name="types">Collection of types to register as.</param>
        public InstanceTypeRegistration(object value, params Type[] types)
            :this(value, RegistrationLifetime.Transient, types)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceTypeRegistration"/> class.
        /// </summary>
        /// <param name="value">Object instance value to register.</param>
        /// <param name="types">Collection of types to register as.</param>
        /// <param name="lifetime">Lifetime to consider for this registration.</param>
        public InstanceTypeRegistration(object value, RegistrationLifetime lifetime, params Type[] types)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            AbstractionTypes = types ?? throw new ArgumentNullException(nameof(types));
            if (!types.Any())
            {
                throw new ArgumentException("InstanceTypeRegistration.ctor() : It's necessary to add at least one type to register as.");
            }
            Lifetime = lifetime;
        }

        #endregion

    }
}
