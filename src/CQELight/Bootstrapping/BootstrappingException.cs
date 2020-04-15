using CQELight.Bootstrapping.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight
{
    /// <summary>
    /// A bootstrapping exception that is throw if bootstrapper
    /// encounters an exception.
    /// </summary>
    public class BootstrappingException : Exception
    {
        #region Properties

        /// <summary>
        /// Collection of notifications.
        /// </summary>
        public IEnumerable<BootstrapperNotification> Notifications { get; } = Enumerable.Empty<BootstrapperNotification>();

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new BootstrappingException with the collection of notifications.
        /// </summary>
        /// <param name="notifications">Collection of notification to use.</param>
        public BootstrappingException(IEnumerable<BootstrapperNotification> notifications)
        {
            Notifications = notifications;
        }

        /// <summary>
        /// Creates a new empty BootstrappingException.
        /// </summary>
        public BootstrappingException() : base()
        {
        }

        /// <summary>
        /// Creates a BootstrappingException with the desired message.
        /// </summary>
        /// <param name="message">Desired message for exception.</param>
        public BootstrappingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a BootstrappingException with the desired message and
        /// the inner exception.
        /// </summary>
        /// <param name="message">Desired message for the exception.</param>
        /// <param name="innerException">Inner exception to use.</param>
        public BootstrappingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion

    }
}
