using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
        public CQELightServiceProvider(IScopeFactory scopeFactory)
        {
            this.scope = scopeFactory.CreateScope();
        }

        #endregion

        #region IServiceProvider methods

        public object GetService(Type serviceType)
            => scope.Resolve(serviceType);

        #endregion

        #region ISupportRequiredService methods

        public object GetRequiredService(Type serviceType)
            => scope.Resolve(serviceType);

        #endregion
    }
}
