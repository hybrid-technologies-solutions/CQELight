using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Bootstrapping.Notifications;
using CQELight.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight.Bootstrapping
{
    /// <summary>
    /// Holding all informations post bootstrapping process.
    /// </summary>
    public sealed class PostBootstrappingContext : DisposableObject
    {
        #region Properties

        /// <summary>
        /// Collection of notifications that has been produced by bootstrapping context.
        /// </summary>
        public IEnumerable<BootstrapperNotification> Notifications { get; internal set; }

        /// <summary>
        /// Flag that indicates if an error has been met during bootstrapping.
        /// </summary>
        public bool IsError => Notifications.Any(n => n.Type == BootstrapperNotificationType.Error);

        /// <summary>
        /// IoC resolution scope, if IoC has been configured.
        /// </summary>
        public IScope Scope { get; internal set; }

        #endregion

        #region Ctor

        internal PostBootstrappingContext()
        {

        }

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            Scope?.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}
