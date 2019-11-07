using CQELight.AspCore.Internal;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight
{
    /// <summary>
    /// Extensions methods for CQELight with ASP.NET Core.
    /// </summary>
    public static class ASPCoreExtensions
    {
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
    }
}
