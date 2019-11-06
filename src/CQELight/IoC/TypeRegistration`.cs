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
        /// Create a new type as type(s) registration for IoC container.
        /// </summary>
        /// <param name="registrationTypes">Registration types.</param>
        public TypeRegistration(params Type[] registrationTypes) 
            : base(typeof(T), registrationTypes)
        {
        }


        /// <summary>
        /// Create a new type as everything possible (self + all interfaces).
        /// </summary>
        /// <param name="forEverything">Flag that indicates if should register has everything possible.</param>
        public TypeRegistration(bool forEverything) : 
            base(typeof(T), forEverything)
        {
        }

        #endregion

    }
}
