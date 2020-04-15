using CQELight.Abstractions.DDD;
using System.Threading.Tasks;

namespace CQELight.Abstractions.CQS.Interfaces
{
    /// <summary>
    /// Contrat interface for dispatching Commands.
    /// </summary>
    public interface ICommandBus
    {
        /// <summary>
        /// Dispatch command asynchrounously.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="context">Context associated to command.</param>
        /// <returns>List of launched tasks from handler.</returns>
        Task<Result> DispatchAsync(ICommand command, ICommandContext? context = null);
    }
}
