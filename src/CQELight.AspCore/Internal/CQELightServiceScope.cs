using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CQELight.AspCore.Internal
{
    class CQELightServiceScope : DisposableObject, IServiceScope
    {
        #region Members

        private IScopeFactory scopeFactory;

        #endregion

        #region Ctor

        public CQELightServiceScope(
            IScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        #endregion

        #region IServiceScope methods

        public IServiceProvider ServiceProvider 
            => new CQELightServiceProvider(scopeFactory.CreateScope());


        #endregion
    }
}
