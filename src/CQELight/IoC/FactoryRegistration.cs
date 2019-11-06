using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public Func<object> Factory { get; private set; }
        /// <summary>
        /// Type to register as.
        /// </summary>
        public IEnumerable<Type> AbstractionTypes { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor for a factory registration.
        /// </summary>
        /// <param name="factory">Object instance value to register.</param>
        /// <param name="types">Collection of types to register as.</param>
        public FactoryRegistration(Func<object> factory, params Type[] types)
        {
            AbstractionTypes = types ?? throw new ArgumentNullException(nameof(types));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            if (types.Length == 0)
            {
                throw new ArgumentException("InstanceTypeRegistration.ctor() : It's necessary to add at least one type to register as.");
            }
        }

        #endregion

    }
}
