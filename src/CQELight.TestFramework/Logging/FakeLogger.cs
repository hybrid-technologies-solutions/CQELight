using CQELight.Tools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.TestFramework.Logging
{
    /// <summary>
    /// A fake logger that can be used to check if log really happens.
    /// </summary>
    public class FakeLogger : DisposableObject, ILogger
    {
        #region Members

        private StringBuilder stringBuilder = new StringBuilder();
        private LogLevel minLogLevel;

        #endregion

        #region Properties

        /// <summary>
        /// The current log value.
        /// </summary>
        public string CurrentLogValue => stringBuilder.ToString();

        /// <summary>
        /// Number of logs that has been produced.
        /// </summary>
        public int NbLogs { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new FakeLogger that accept every LogLevel
        /// </summary>
        public FakeLogger() { minLogLevel = LogLevel.Trace; }

        /// <summary>
        /// Creates a new FakeLogger that accept logs where level is greater of equal to the provided one.
        /// </summary>
        /// <param name="minLogLevel"></param>
        public FakeLogger(LogLevel minLogLevel)
        {
            this.minLogLevel = minLogLevel;
        }

        #endregion

        #region ILogger

        public IDisposable BeginScope<TState>(TState state)
            => this;

        public bool IsEnabled(LogLevel logLevel)
            => (int)logLevel >= (int)minLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            NbLogs++;
            stringBuilder.Append($"{logLevel} : {state}");
        }

        #endregion
    }
}
