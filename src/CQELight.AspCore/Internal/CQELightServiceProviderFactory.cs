using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.AspCore.Internal
{
    class CQELightServiceProviderFactory : IServiceProviderFactory<IScopeFactory>
    {
        #region Members

        private readonly Bootstrapper bootstrapper;

        #endregion

        #region Ctor

        public CQELightServiceProviderFactory(Bootstrapper bootstrapper)
        {
            this.bootstrapper = bootstrapper;
        }

        #endregion

        #region IServiceProviderFactory methods

        public IScopeFactory CreateBuilder(IServiceCollection services)
        {
            bootstrapper.AddIoCRegistration(new TypeRegistration<CQELightServiceProvider>(true));
            bootstrapper.AddIoCRegistration(new TypeRegistration<CQELightServiceScopeFactory>(true));

            foreach (var item in services)
            {
                if (item.ServiceType != null)
                {
                    if (item.ImplementationType != null)
                    {
                        bootstrapper.AddIoCRegistration(new TypeRegistration(item.ImplementationType,
                            item.Lifetime == ServiceLifetime.Singleton ? RegistrationLifetime.Singleton : RegistrationLifetime.Transient,
                            TypeResolutionMode.OnlyUsePublicCtors, item.ServiceType));
                    }
                    else if (item.ImplementationFactory != null)
                    {
                        bootstrapper.AddIoCRegistration(new FactoryRegistration(() => item.ImplementationFactory(
                            new CQELightServiceProvider(DIManager.BeginScope().Resolve<IScopeFactory>())), item.ServiceType));
                    }
                    else if (item.ImplementationInstance != null)
                    {
                        bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(item.ImplementationInstance, item.ServiceType));
                    }
                }
            }
            bootstrapper.Bootstrapp();
            return DIManager.BeginScope().Resolve<IScopeFactory>();
        }

        public IServiceProvider CreateServiceProvider(IScopeFactory containerBuilder)
        {
            return new CQELightServiceProvider(containerBuilder);
        }

        #endregion

    }
}
