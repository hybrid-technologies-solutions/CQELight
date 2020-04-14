using System;

namespace CQELight.MVVM
{
    /// <summary>
    /// Type of alerts.
    /// </summary>
    public enum AlertType
    {
        /// <summary>
        /// Information.
        /// </summary>
        Info,
        /// <summary>
        /// Warning.
        /// </summary>
        Warning,
        /// <summary>
        /// Error.
        /// </summary>
        Error,
        /// <summary>
        /// Question
        /// </summary>
        Question
    }

    /// <summary>
    /// Options for displaying message dialogs.
    /// </summary>
    public class MessageDialogServiceOptions
    {
        #region Properties

        /// <summary>
        /// Style de dialogue à afficher.
        /// </summary>
        public AlertType DialogStyle { get; set; } = AlertType.Info;

        /// <summary>
        /// Show a cancel option.
        /// </summary>
        public bool ShowCancel { get; set; }

        /// <summary>
        /// Callback to invoke when user is cancelling.
        /// </summary>
        public Action? CancelCallback { get; set; }

        #endregion

    }
}
