using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.IoC
{
    /// <summary>
    /// Lifetime of the registration.
    /// </summary>
    public enum RegistrationLifetime
    {
        /// <summary>
        /// An instance is retrieved every resolve.
        /// </summary>
        Transient,
        /// <summary>
        /// An single instance is retrieved for each resolve within the same scope.
        /// </summary>
        Scoped,
        /// <summary>
        /// A single instance is retrieved for each resolve.
        /// </summary>
        Singleton
    }
}
