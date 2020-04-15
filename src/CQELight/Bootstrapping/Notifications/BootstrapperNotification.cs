using System;

namespace CQELight.Bootstrapping.Notifications
{
    /// <summary>
    /// Enumeration of notification type
    /// </summary>
    public enum BootstrapperNotificationType
    {
        /// <summary>
        /// Notification of type 'Info'.
        /// </summary>
        Info,
        /// <summary>
        /// Notification of type 'Warning'.
        /// </summary>
        Warning,
        /// <summary>
        /// Notififcation of type 'Error'.
        /// </summary>
        Error
    }
    /// <summary>
    /// Enumeration of notification content type.
    /// </summary>
    public enum BootstapperNotificationContentType
    {
        /// <summary>
        /// Type of notification when IoC service is not registered.
        /// </summary>
        IoCServiceMissing,
        /// <summary>
        /// Type of notification when DAL service is not registered.
        /// </summary>
        DALServiceMissing,
        /// <summary>
        /// Type of notification when EventStore service is not registered.
        /// </summary>
        EventStoreServiceMissing,
        /// <summary>
        /// Type of notification when Bus service is not registered.
        /// </summary>
        BusServiceMissing,
        /// <summary>
        /// Some IoC registrations has been made by someone but there's
        /// not IoC service to handle them
        /// </summary>
        IoCRegistrationsHasBeenMadeButNoIoCService,
        /// <summary>
        /// Custom notification added by a service.
        /// </summary>
        CustomServiceNotification
    }

    /// <summary>
    /// Notification of bootstrapper.
    /// </summary>
    public class BootstrapperNotification
    {
        #region Properties

        /// <summary>
        /// Type of notification.
        /// </summary>
        public BootstrapperNotificationType Type { get; internal set; }
        /// <summary>
        /// Content type of the notification.
        /// </summary>
        public BootstapperNotificationContentType ContentType { get; internal set; }
        /// <summary>
        /// Message of the current notification, if any.
        /// </summary>
        public string Message { get; } = "";
        /// <summary>
        /// Type of service that have created this notification.
        /// </summary>
        public Type? BootstrapperServiceType { get; }

        #endregion

        #region Ctor

        internal BootstrapperNotification()
        {
        }

        /// <summary>
        /// Creates a new notification with a provided message.
        /// This will set ContentType to 'CustomServiceNotification'.
        /// </summary>
        /// <param name="type">Type of notification to add.</param>
        /// <param name="message">Message to insert in notification.</param>
        public BootstrapperNotification(BootstrapperNotificationType type, string message)
        {
            Message = message;
            Type = type;
            ContentType = BootstapperNotificationContentType.CustomServiceNotification;
        }

        /// <summary>
        /// Creates a new notification with a provided message and
        /// a specified bootstrapperService type.
        /// This will set ContentType to 'CustomServiceNotification'.
        /// </summary>
        /// <param name="type">Type of notification to add.</param>
        /// <param name="bootstrapperServiceType">Type of bootstrapperService that have
        /// generated this notification/</param>
        /// <param name="message">Message to insert in notification.</param>
        public BootstrapperNotification(BootstrapperNotificationType type, string message, Type bootstrapperServiceType)
            : this(type, message)
        {
            BootstrapperServiceType
                = bootstrapperServiceType ?? throw new ArgumentNullException(nameof(bootstrapperServiceType));
        }

        #endregion

    }
}
