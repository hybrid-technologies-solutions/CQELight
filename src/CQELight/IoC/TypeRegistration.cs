using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public IEnumerable<Type> AbstractionTypes { get => _abstractionTypes.AsEnumerable(); }

        /// <summary>
        /// Instance type.
        /// </summary>
        public Type InstanceType { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new type as type(s) registration for IoC container.
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="registrationTypes">Registration types.</param>
        public TypeRegistration(Type instanceType, params Type[] registrationTypes)
        {
            InstanceType = instanceType ?? throw new ArgumentNullException(nameof(instanceType));
            _abstractionTypes = registrationTypes?.ToList() ?? new List<Type>();
            _abstractionTypes.Add(instanceType);
        }

        /// <summary>
        /// Create a new type as everything possible (self + all interfaces).
        /// </summary>
        /// <param name="instanceType">Type instance.</param>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        public TypeRegistration(Type instanceType, bool forEverything)
        {
            InstanceType = instanceType;
            if (forEverything)
            {
                _abstractionTypes.Add(instanceType);
                _abstractionTypes.AddRange(instanceType.GetInterfaces());
            }

        }

        #endregion

    }
}
