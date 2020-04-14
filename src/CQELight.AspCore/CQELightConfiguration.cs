using CQELight.Bootstrapping.Notifications;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.AspCore
{
    public static class CQELightConfiguration
    {
        #region Public static methods

        /// <summary>
        /// Allow an AspCore to easily configure CQELight's bootstrapper.
        /// Note that this will use AspCore dependency injection system if not
        /// IoC extensions are registered.
        /// DO NOT CALL bootstrapp method, because calling Bootstrapp method on bootstrapper is done
        /// in this method after several other things.
        /// To ensure that bootstrapper is correctly configured, call it as last method within your ConfigureServices
        /// method.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <param name="bootstrapperAction">Bootstrapper configuration action.</param>
        /// <param name="strict">Specifiy that bootstrapping should apply strict verification of configuration</param>
        /// <param name="optimal">Specify that bootstrapping should check that system has been configured in an optimal way</param>
        /// <param name="throwOnError">Specifiy that if bootsrapper generates any error notification, it should throw exception instead of just returning</param>
        /// <returns>A collection of notifications, depending on how the system has been configured</returns>
        [Obsolete("Will be removed in further version, use ConfigureCQELight instead")]
        public static IEnumerable<BootstrapperNotification> AddCQELight(
            this IServiceCollection services,
            Action<Bootstrapper> bootstrapperAction,
            bool strict = false,
            bool optimal = false,
            bool throwOnError = false)
            => AddCQELight(services, bootstrapperAction, new BootstrapperOptions { Strict = strict, CheckOptimal = optimal, ThrowExceptionOnErrorNotif = throwOnError });

        /// <summary>
        /// Allow an AspCore to easily configure CQELight's bootstrapper.
        /// Note that this will use AspCore dependency injection system if not
        /// IoC extensions are registered.
        /// DO NOT CALL bootstrapp method, because calling Bootstrapp method on bootstrapper is done
        /// in this method after several other things.
        /// To ensure that bootstrapper is correctly configured, call it as last method within your ConfigureServices
        /// method.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <param name="bootstrapperAction">Bootstrapper configuration action.</param>
        /// <param name="bootstrapperOptions">Options to configure Bootstrapper</param>
        /// <returns>A collection of notifications, depending on how the system has been configured</returns>
        [Obsolete("Will be removed in further version, use ConfigureCQELight instead")]
        public static IEnumerable<BootstrapperNotification> AddCQELight(
            this IServiceCollection services,
            Action<Bootstrapper> bootstrapperAction,
            BootstrapperOptions? bootstrapperOptions = null)
        {
            if (bootstrapperAction == null)
            {
                throw new ArgumentNullException(nameof(bootstrapperAction));
            }
            var bootstrapper = bootstrapperOptions != null ? new Bootstrapper(bootstrapperOptions) : new Bootstrapper();
            bootstrapperAction(bootstrapper);
            ConfigureBootstrapperIfNeeded(bootstrapper, services);
            return bootstrapper.Bootstrapp();
        }

        /// <summary>
        /// Allow an AspCore website to use CQELight system by taking a reference
        /// to a configured bootstrapper.
        /// DO NOT CALL bootstrapp method before, because calling Bootstrapp method on bootstrapper is done
        /// in this method after several other things.
        /// To ensure that bootstrapper is correctly configured, call it as last method within your ConfigureServices
        /// method.
        /// </summary>
        /// <param name="services">AspCore service collection</param>
        /// <param name="bootstrapper">Bootstrapper instance</param>
        /// <returns>A collection of notifications, depending on how the system has been configured</returns>
        [Obsolete("Will be removed in further version, use ConfigureCQELight instead")]
        public static IEnumerable<BootstrapperNotification> AddCQELight(
            this IServiceCollection services,
            Bootstrapper bootstrapper)
        {
            if (bootstrapper == null)
            {
                throw new ArgumentNullException(nameof(bootstrapper));
            }
            ConfigureBootstrapperIfNeeded(bootstrapper, services);
            return bootstrapper.Bootstrapp();
        }

        #endregion

        #region Private methods

        private static void ConfigureBootstrapperIfNeeded(Bootstrapper bootstrapper, IServiceCollection services)
        {
            if (!bootstrapper.RegisteredServices.Any(s => s.ServiceType == BootstrapperServiceType.IoC))
            {
                bootstrapper.UseMicrosoftDependencyInjection(services);
            }
        }

        #endregion

    }
}
