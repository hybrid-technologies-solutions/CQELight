using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC;
using CQELight.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

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
            bootstrapper.AddIoCRegistration(new TypeRegistration<CQELightServiceProvider>(typeof(IServiceProvider), typeof(ISupportRequiredService)));
            bootstrapper.AddIoCRegistration(new TypeRegistration<CQELightServiceProviderFactory>(typeof(IServiceProviderFactory<IScopeFactory>)));
            bootstrapper.AddIoCRegistration(new TypeRegistration<CQELightServiceScope>(typeof(IServiceScope)));

            RegistrationLifetime GetLifetimeFromServiceLifetime(ServiceLifetime lifetime)
                => lifetime switch
                {
                    ServiceLifetime.Scoped => RegistrationLifetime.Scoped,
                    ServiceLifetime.Singleton => RegistrationLifetime.Singleton,
                    _ => RegistrationLifetime.Transient
                };

            if (!bootstrapper.RegisteredServices.Any(s => s.ServiceType == BootstrapperServiceType.IoC))
            {
                bootstrapper.UseMicrosoftDependencyInjection(services);
            }
            else
            {
                foreach (var item in services)
                {
                    if (item.ServiceType != null)
                    {
                        if (item.ImplementationType != null)
                        {
                            bootstrapper.AddIoCRegistration(new TypeRegistration(item.ImplementationType,
                                GetLifetimeFromServiceLifetime(item.Lifetime),
                                TypeResolutionMode.OnlyUsePublicCtors, item.ServiceType));
                        }
                        else if (item.ImplementationFactory != null)
                        {
                            bootstrapper.AddIoCRegistration(new FactoryRegistration(s => item.ImplementationFactory(
                                new CQELightServiceProvider(s)), item.ServiceType));
                        }
                        else if (item.ImplementationInstance != null)
                        {
                            bootstrapper.AddIoCRegistration(new InstanceTypeRegistration(item.ImplementationInstance, item.ServiceType));
                        }
                    }
                }
            }
            bootstrapper.Bootstrapp();
            return DIManager._scopeFactory;
        }

        public IServiceProvider CreateServiceProvider(IScopeFactory scopeFactory)
        {
            return new CQELightServiceProvider(scopeFactory);
        }

        #endregion

    }
}
