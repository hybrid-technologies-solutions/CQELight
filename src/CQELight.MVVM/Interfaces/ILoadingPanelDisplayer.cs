using CQELight.MVVM.Common;
using System.Threading.Tasks;

namespace CQELight.MVVM.Interfaces
{
    /// <summary>
    /// Contract interface for waiting/loading panel display.
    /// </summary>
    public interface ILoadingPanelDisplayer
    {
        /// <summary>
        /// Show a loading panel on the view with specified message.
        /// </summary>
        /// <param name="waitMessage">Message.</param>
        /// <param name="options">Options for loading panel.</param>
        Task ShowLoadingPanelAsync(string waitMessage, LoadingPanelOptions? options = null);
        /// <summary>
        /// Hide the loading panel.
        /// </summary>
        Task HideLoadingPanelAsync();
    }
}
