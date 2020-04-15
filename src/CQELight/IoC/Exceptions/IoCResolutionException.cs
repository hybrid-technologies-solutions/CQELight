using System;

namespace CQELight.IoC.Exceptions
{
    /// <summary>
    /// A generic exception that indicates an IoC resolution exception.
    /// </summary>
    public class IoCResolutionException : Exception
    {
        #region Ctor

        /// <summary>
        /// Creates a new empty IoCResolutionException.
        /// </summary>
        public IoCResolutionException()
            : base()
        {
        }

        /// <summary>
        /// Creates a new IoCResolutionException with a specific message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public IoCResolutionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new IoCResolutionException with a specific message
        /// and the IoC provider specific exception
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">IoC provider specific exception.</param>
        public IoCResolutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion

    }
}
