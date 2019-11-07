using CQELight.Abstractions.IoC.Interfaces;
using CQELight.AspCore.Internal;
#if NETSTANDARD2_1
using Microsoft.Extensions.Hosting;
#elif NETSTANDARD2_0
using Microsoft.Extensions.DependencyInjection;
#endif
using System;

namespace CQELight
{
    /// <summary>
    /// Extensions methods for CQELight with ASP.NET Core.
    /// </summary>
    public static class ASPCoreExtensions
    {
#if NETSTANDARD2_1
        /// <summary>
        /// Configures CQELight to work with ASP.NET Core WebSite.
        /// </summary>
        /// <param name="hostBuilder">ASP.NET Core host builder</param>
        /// <param name="bootstrapperConf">Bootstrapper configuration method.</param>
        /// <param name="bootstrapperOptions">Bootstrapper options.</param>
        /// <returns>Configured ASP.NET Core host builder</returns>
        public static IHostBuilder ConfigureCQELight(
            this IHostBuilder hostBuilder, 
            Action<Bootstrapper> bootstrapperConf,
            BootstrapperOptions bootstrapperOptions = null)
        {
            if (bootstrapperConf == null)
            {
                throw new ArgumentNullException(nameof(bootstrapperConf));
            }

            var bootstrapper = bootstrapperOptions != null ? new Bootstrapper(bootstrapperOptions) : new Bootstrapper();
            bootstrapperConf.Invoke(bootstrapper);
            return hostBuilder.UseServiceProviderFactory(new CQELightServiceProviderFactory(bootstrapper));
        }
#elif NETSTANDARD2_0
        /// <summary>
        /// Configures CQELight to work with ASP.NET Core WebSite.
        /// </summary>
        /// <param name="services">Current ASP.NET service collection</param>
        /// <param name="bootstrapperConf">Bootstrapper configuration method.</param>
        /// <param name="bootstrapperOptions">Bootstrapper options.</param>
        /// <returns>Configured ASP.NET Core host builder</returns>
        public static IServiceProvider ConfigureCQELight(
            this IServiceCollection services, 
            Action<Bootstrapper> bootstrapperConf,
            BootstrapperOptions bootstrapperOptions = null)
        {
            if (bootstrapperConf == null)
            {
                throw new ArgumentNullException(nameof(bootstrapperConf));
            }

            var bootstrapper = bootstrapperOptions != null ? new Bootstrapper(bootstrapperOptions) : new Bootstrapper();
            bootstrapperConf.Invoke(bootstrapper);
            services.AddSingleton<IServiceProviderFactory<IScopeFactory>>(new CQELightServiceProviderFactory(bootstrapper));
            var tempFactory = services.BuildServiceProvider().GetService<IServiceProviderFactory<IScopeFactory>>();
            var scopeFactory = tempFactory.CreateBuilder(services);
            return tempFactory.CreateServiceProvider(scopeFactory);
        }
#endif
    }
}
