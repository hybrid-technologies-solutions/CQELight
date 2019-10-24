using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CQELight.Tools.Extensions
{
    /// <summary>
    /// Some extensions methods for ILogger.
    /// </summary>
    public static class ILoggerExtensions
    {
        #region Public static methods

        /// <summary>
        /// Log an error on multiples lines.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="errorLines">Collection of lines to log as error.</param>
        public static void LogErrorMultilines(this ILogger logger, params string[] errorLines)
            => logger.LogError(string.Join(Environment.NewLine, errorLines));

        /// <summary>
        /// Log a warning on multiples lines.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="warningLines">Collection of lines to log as warning.</param>
        public static void LogWarningMultilines(this ILogger logger, params string[] warningLines)
            => logger.LogWarning(string.Join(Environment.NewLine, warningLines));

        /// <summary>
        /// Log current thread info to the logger, as debug.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public static void LogThreadInfos(this ILogger logger)
        {
            logger.LogDebug(() => $"Thread infos :{Environment.NewLine}");
            logger.LogDebug(() => $"id = {Thread.CurrentThread.ManagedThreadId}{Environment.NewLine}");
            logger.LogDebug(() => $"priority = {Thread.CurrentThread.Priority}{Environment.NewLine}");
            logger.LogDebug(() => $"name = {Thread.CurrentThread.Name}{Environment.NewLine}");
            logger.LogDebug(() => $"state = {Thread.CurrentThread.ThreadState}{Environment.NewLine}");
            logger.LogDebug(() => $"culture = {Thread.CurrentThread.CurrentCulture?.Name}{Environment.NewLine}");
            logger.LogDebug(() => $"ui culture = {Thread.CurrentThread.CurrentUICulture?.Name}{Environment.NewLine}");
        }

        /// <summary>
        /// Perform a lazy log of a debug value, only if debug level is enabled.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="logRetriever">Lambda for retrieving log value</param>
        public static void LogDebug(this ILogger logger, Func<string> logRetriever)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                try
                {
                    logger.LogDebug(logRetriever());
                }
                catch
                {
                    //No need to throw for logging purpose
                }
            }
        }

        /// <summary>
        /// Perform a lazy log of an information value, only if debug level is enabled.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="logRetriever">Lambda for retrieving log value</param>
        public static void LogInformation(this ILogger logger, Func<string> logRetriever)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                try
                {
                    logger.LogInformation(logRetriever());
                }
                catch 
                {
                    //No need to throw for logging purpose
                }
            }
        }

        /// <summary>
        /// Perform a lazy log of a warning value, only if debug level is enabled.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="logRetriever">Lambda for retrieving log value</param>
        public static void LogWarning(this ILogger logger, Func<string> logRetriever)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(logRetriever());
            }
        }
        #endregion

    }
}
