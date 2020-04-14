using System.Threading.Tasks;

namespace CQELight.MVVM.Interfaces
{
    /// <summary>
    /// Contract interface for displaying message on views.
    /// </summary>
    public interface IMessageDialogService
    {
        /// <summary>
        /// Pops a yes/no question to the user and get the answeR.
        /// </summary>
        /// <param name="title">Title of message.</param>
        /// <param name="message">Message to prompt.</param>
        /// <param name="options">Options.</param>
        /// <returns>True if user answer yes, false otherwise.</returns>
        Task<bool> ShowYesNoDialogAsync(string title, string message, MessageDialogServiceOptions? options = null);

        /// <summary>
        /// Show an alert to the user.
        /// </summary>
        /// <param name="title">Title of the alert.</param>
        /// <param name="message">Message to show.</param>
        /// <param name="options">Options.</param>
        Task ShowAlertAsync(string title, string message, MessageDialogServiceOptions? options = null);
    }
}