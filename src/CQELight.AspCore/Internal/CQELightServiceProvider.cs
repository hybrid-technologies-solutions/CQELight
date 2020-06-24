using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CQELight.AspCore.Internal
{
    class CQELightServiceProvider : DisposableObject, IServiceProvider, ISupportRequiredService
    {
        #region Members

        private readonly IScope scope;

        #endregion

        #region Ctor

        public CQELightServiceProvider(IScope scope)
        {
            this.scope = scope;
        }

        #endregion

        #region IServiceProvider methods

        public object? GetService(Type serviceType)
            => scope.Resolve(serviceType);

        #endregion

        #region ISupportRequiredService methods

        public object GetRequiredService(Type serviceType)
            => scope.ResolveRequired(serviceType);

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            scope?.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}
