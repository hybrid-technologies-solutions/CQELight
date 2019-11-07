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
        /// An single unique instance is retrieved for each resolve within the same scope.
        /// </summary>
        Scoped,

        /// <summary>
        /// A single unique instance is retrieved for each resolve.
        /// </summary>
        Singleton
    }

    /// <summary>
    /// Kind of type resolution mode (when type is resolved per reflection)
    /// </summary>
    public enum TypeResolutionMode
    {
        /// <summary>
        /// Only use public ctors.
        /// </summary>
        OnlyUsePublicCtors,

        /// <summary>
        /// If container is capable of, use every possible ctor, even private or protected ones.
        /// </summary>
        Full
    }
}
