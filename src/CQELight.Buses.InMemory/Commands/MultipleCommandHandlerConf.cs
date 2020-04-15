using System;

namespace CQELight.Buses.InMemory.Commands
{
    /// <summary>
    /// Definition for multiple command handler.
    /// </summary>
    public class MultipleCommandHandlerConf
    {
        #region Properties
        /// <summary>
        /// Concerned type of the command.
        /// </summary>
        public Type CommandType { get; }
        /// <summary>
        /// Flag that indicates if handler should wait before going to the next one.
        /// </summary>
        public bool ShouldWait { get; internal set; }

        #endregion

        #region Ctor

        internal MultipleCommandHandlerConf(Type commandType)
        {
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
        }

        #endregion

    }
}
