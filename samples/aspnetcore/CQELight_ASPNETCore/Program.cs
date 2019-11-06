using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CQELight;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC;
using CQELight.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static CQELight_ASPNETCore.Program;

namespace CQELight_ASPNETCore
{
    public class Program
    {
        public class DIBuilder
        {
            private List<ITypeRegistration> registrations = new List<ITypeRegistration>();
            public IEnumerable<ITypeRegistration> Registrations => registrations.AsEnumerable();
            public void AddIoCRegistration(ITypeRegistration registration)
            {
                if (registration == null)
                {
                    throw new ArgumentNullException(nameof(registration));
                }
                registrations.Add(registration);
            }
            public IScope CreateScope()
            {
                return DIManager.BeginScope();
            }
        }

        public class CQELightServiceProviderFactory : IServiceProviderFactory<IScopeFactory>
        {
            private readonly Bootstrapper bootstrapper;

            public CQELightServiceProviderFactory(Bootstrapper bootstrapper)
            {
                this.bootstrapper = bootstrapper;
            }
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
                                item.ServiceType));
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
        }

        public class CQELightServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IScopeFactory scopeFactory;

            public CQELightServiceScopeFactory(IScopeFactory scopeFactory)
            {
                this.scopeFactory = scopeFactory;
            }
            public IServiceScope CreateScope()
                => new CQELightServicesScope(scopeFactory);
        }

        public class CQELightServicesScope : IServiceScope
        {
            private IScopeFactory scopeFactory;

            public CQELightServicesScope(IScopeFactory scopeFactory)
            {
                this.scopeFactory = scopeFactory;
            }

            public IServiceProvider ServiceProvider => new CQELightServiceProvider(scopeFactory);

            public void Dispose()
            {
            }
        }

        public class CQELightServiceProvider : DisposableObject, IServiceProvider, ISupportRequiredService, IDisposable
        {
            private readonly IScope scope;

            public CQELightServiceProvider(IScope scope)
            {
                this.scope = scope;
            }
            public CQELightServiceProvider(IScopeFactory scopeFactory)
            {
                this.scope = scopeFactory.CreateScope();
            }
            public object GetRequiredService(Type serviceType)
                => scope.Resolve(serviceType);

            public object GetService(Type serviceType)
                => scope.Resolve(serviceType);
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                .ConfigureCQELight(b => b.UseAutofacAsIoC())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
    public static class e
    {
        public static IHostBuilder ConfigureCQELight(this IHostBuilder hostBuilder, Action<Bootstrapper> bootstrapperConf)
        {
            var bootstrapper = new Bootstrapper();
            bootstrapperConf.Invoke(bootstrapper);
            return hostBuilder.UseServiceProviderFactory(new CQELightServiceProviderFactory(bootstrapper));
        }
    }

}
