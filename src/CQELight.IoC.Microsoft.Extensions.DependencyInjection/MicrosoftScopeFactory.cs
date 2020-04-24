using CQELight.Abstractions.IoC.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CQELight.IoC.Microsoft.Extensions.DependencyInjection
{
    internal class MicrosoftScopeFactory : IScopeFactory
    {
        #region Members

        private readonly IServiceProvider serviceProvider;
        private readonly IServiceCollection services;

        #endregion

        #region Ctor

        public MicrosoftScopeFactory(IServiceCollection services)
        {
            serviceProvider = services.BuildServiceProvider();
            this.services = services;
        }

        public MicrosoftScopeFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        #endregion

        #region IScopeFactory

        public IScope CreateScope()
            => new MicrosoftScope(serviceProvider.CreateScope(), services);

        #endregion
    }
}
