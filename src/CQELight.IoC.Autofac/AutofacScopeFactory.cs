using Autofac;
using CQELight.Abstractions.IoC.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace CQELight.IoC.Autofac
{
    internal class AutofacScopeFactory : IScopeFactory
    {
        #region Members

        private readonly ILifetimeScope rootScope;
        private readonly ILoggerFactory? loggerFactory;

        #endregion

        #region Static properties

        internal static IContainer? AutofacContainer { get; set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="autofacContainer">Autofac container.</param>
        public AutofacScopeFactory(ILifetimeScope autofacContainer)
        {
            rootScope = autofacContainer ?? throw new ArgumentNullException(nameof(autofacContainer),
                "AutofacScopeFactory.ctor() : Autofac container should be provided.");
        }

        /// <summary>
        /// Default constructor with a logger instance.
        /// </summary>
        /// <param name="autofacContainer">Autofac container.</param>
        /// <param name="loggerFactory">Logger factory</param>
        public AutofacScopeFactory(ILifetimeScope autofacContainer, ILoggerFactory loggerFactory)
            : this(autofacContainer)
        {
            this.loggerFactory = loggerFactory;
        }

        #endregion

        #region IScopeFactory methods

        /// <summary>
        /// Create a new scope.
        /// </summary>
        /// <returns>New instance of scope.</returns>
        public IScope CreateScope()
            => loggerFactory != null
                ? new AutofacScope(rootScope.BeginLifetimeScope(), loggerFactory.CreateLogger<AutofacScope>())
                : new AutofacScope(rootScope.BeginLifetimeScope());

        #endregion

    }
}
