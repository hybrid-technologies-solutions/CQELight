using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQELight.TestFramework.Fakes.Buses
{
    /// <summary>
    /// Fake command bus used to test commands.
    /// </summary>
    public class FakeCommandBus : ICommandBus
    {
        #region Members

        internal IList<ICommand> _commands = new List<ICommand>();
        private readonly Result? expectedResult;

        #endregion

        #region Properties

        /// <summary>
        /// List of all commands.
        /// </summary>
        public IEnumerable<ICommand> Commands => _commands.AsEnumerable();

        #endregion

        #region Ctor

        /// <summary>
        /// Creates an new <see cref="FakeCommandBus"/> with expected result for dispatch calls.
        /// </summary>
        /// <param name="expectedResult">Predefined result to returns. If null, ok will be returned</param>
        public FakeCommandBus(Result? expectedResult = null)
        {
            this.expectedResult = expectedResult;
        }

        #endregion

        #region ICommandBus

        /// <summary>
        /// Dispatch command asynchrounously.
        /// </summary>
        /// <param name="command">Command to dispatch.</param>
        /// <param name="context">Context associated to command.</param>
        /// <returns>List of launched tasks from handler.</returns>
        public Task<Result> DispatchAsync(ICommand command, ICommandContext? context = null)
        {
            _commands.Add(command);
            return Task.FromResult(expectedResult ?? Result.Ok());
        }

        #endregion

    }
}
