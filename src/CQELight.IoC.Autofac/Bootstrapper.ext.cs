using Autofac;
using Autofac.Builder;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC;
using CQELight.IoC.Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQELight
{
    public static class BootstrapperExt
    {
        #region Public static methods

        /// <summary>
        /// Configure the bootstrapper to use Autofac as IoC.
        /// </summary>
        /// <param name="bootstrapper">Instance of boostrapper.</param>
        /// <param name="containerBuilder">Autofac containerbuilder that has been configured according to app..</param>
        /// <param name="excludedAutoRegisterTypeDLLs">DLLs name to exclude from auto-configuration into IoC
        /// (IAutoRegisterType will be ineffective).</param>
        public static Bootstrapper UseAutofacAsIoC(this Bootstrapper bootstrapper, ContainerBuilder containerBuilder,
            params string[] excludedAutoRegisterTypeDLLs)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }
            var service = new AutofacBootstrappService
            {
                BootstrappAction = (ctx) => CreateConfigWithContainer(bootstrapper, containerBuilder, excludedAutoRegisterTypeDLLs)
            };
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        /// <summary>
        /// Configure the bootstrapper to use Autofac as IoC, without custom registrations.
        /// Only system and plugin registrations will be added.
        /// </summary>
        /// <param name="bootstrapper">Instance of boostrapper.</param>
        /// <param name="excludedAutoRegisterTypeDLLs">DLLs name to exclude from auto-configuration into IoC
        /// (IAutoRegisterType will be ineffective).</param>
        public static Bootstrapper UseAutofacAsIoC(this Bootstrapper bootstrapper, params string[] excludedAutoRegisterTypeDLLs)
            => UseAutofacAsIoC(bootstrapper, _ => { }, excludedAutoRegisterTypeDLLs);

        /// <summary>
        /// Configure the bootstrapper to use Autofac as IoC.
        /// </summary>
        /// <param name="bootstrapper">Instance of boostrapper.</param>
        /// <param name="containerBuilderConfiguration">Configuration to apply on freshly created container builder.</param>
        /// <param name="excludedAutoRegisterTypeDLLs">DLLs name to exclude from auto-configuration into IoC
        /// (IAutoRegisterType will be ineffective).</param>
        public static Bootstrapper UseAutofacAsIoC(this Bootstrapper bootstrapper, Action<ContainerBuilder> containerBuilderConfiguration,
            params string[] excludedAutoRegisterTypeDLLs)
        {
            var service = new AutofacBootstrappService
            {
                BootstrappAction = (ctx) =>
                {
                    ConfigureAutofacContainer(bootstrapper, containerBuilderConfiguration, excludedAutoRegisterTypeDLLs);
                }
            };
            bootstrapper.AddService(service);
            return bootstrapper;
        }
        
        /// <summary>
        /// Configure the bootstrapper to use Autofac as IoC, by using
        /// a defining a scope to be used a root scope for CQELight.
        /// BEWARE : The scope should be kept alive in order to allow the system to work,
        /// because if it's disposed, you will not be able to use CQELight IoC.
        /// </summary>
        /// <param name="bootstrapper">Instance of boostrapper.</param>
        /// <param name="scope">Scope instance</param>
        /// <param name="excludedAutoRegisterTypeDLLs">DLLs name to exclude from auto-configuration into IoC
        /// (IAutoRegisterType will be ineffective).</param>
        /// <returns>Configured bootstrapper</returns>
        public static Bootstrapper UseAutofacAsIoC(this Bootstrapper bootstrapper, ILifetimeScope scope,
            params string[] excludedAutoRegisterTypeDLLs)
        {
            var service = new AutofacBootstrappService
            {
                BootstrappAction = (ctx) =>
                {
                    var childScope =
                        scope.BeginLifetimeScope(cb => AddRegistrationsToContainerBuilder(bootstrapper, cb, excludedAutoRegisterTypeDLLs));
                    InitDIManagerAndCreateScopeFactory(childScope);
                }
            };
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        #endregion

        #region Internal static methods

        internal static void ConfigureAutofacContainer(
            Bootstrapper bootstrapper, 
            Action<ContainerBuilder> containerBuilderConfiguration, 
            string[] excludedAutoRegisterTypeDLLs)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilderConfiguration?.Invoke(containerBuilder);
            CreateConfigWithContainer(bootstrapper, containerBuilder, excludedAutoRegisterTypeDLLs);
        }

        #endregion

        #region Private static methods

        private static void CreateConfigWithContainer(Bootstrapper bootstrapper, ContainerBuilder containerBuilder, string[] excludedAutoRegisterTypeDLLs)
        {
            AddRegistrationsToContainerBuilder(bootstrapper, containerBuilder, excludedAutoRegisterTypeDLLs);
            InitDIManagerAndCreateScopeFactory(containerBuilder.Build());
        }

        private static void InitDIManagerAndCreateScopeFactory(ILifetimeScope scope)
        {
            var factory = new AutofacScopeFactory(scope);
            DIManager.Init(factory);
        }

        private static void AddRegistrationsToContainerBuilder(Bootstrapper bootstrapper, ContainerBuilder containerBuilder, string[] excludedAutoRegisterTypeDLLs)
        {

            containerBuilder.RegisterModule(new AutoRegisterModule(excludedAutoRegisterTypeDLLs));
            AddComponentRegistrationToContainer(containerBuilder, bootstrapper.IoCRegistrations);
            containerBuilder.Register(c => AutofacScopeFactory.Instance).AsImplementedInterfaces();
        }

        private static void AddComponentRegistrationToContainer(ContainerBuilder containerBuilder, IEnumerable<ITypeRegistration> customRegistration)
        {
            if (customRegistration?.Any() == false)
            {
                return;
            }
            var fullCtorFinder = new FullConstructorFinder();
            foreach (var item in customRegistration)
            {
                if (item is InstanceTypeRegistration instanceTypeRegistration)
                {
                    AddLifetime(
                        containerBuilder
                            .Register(c => instanceTypeRegistration.Value)
                            .As(instanceTypeRegistration.AbstractionTypes.ToArray()),
                        instanceTypeRegistration.Lifetime);
                    
                }
                else if (item is TypeRegistration typeRegistration)
                {
                    foreach (var serviceType in typeRegistration.AbstractionTypes)
                    {
                        if (serviceType.IsGenericTypeDefinition)
                        {
                            AddLifetime(
                                containerBuilder
                                    .RegisterGeneric(typeRegistration.InstanceType)
                                    .As(serviceType)
                                    .FindConstructorsWith(new FullConstructorFinder()),
                                typeRegistration.Lifetime);
                        }
                        else
                        {
                            AddLifetime(
                                containerBuilder
                                    .RegisterType(typeRegistration.InstanceType)
                                    .As(serviceType)
                                    .FindConstructorsWith(new FullConstructorFinder()),
                                typeRegistration.Lifetime);
                        }
                    }
                }
                else if (item is FactoryRegistration factoryRegistration)
                {
                    AddLifetime(
                        containerBuilder
                            .Register(c => factoryRegistration.Factory.Invoke())
                            .As(factoryRegistration.AbstractionTypes.ToArray()),
                        factoryRegistration.Lifetime);
                }
            }
        }

        private static void AddLifetime<TLimit, TActivatorData, TRegistrationStyle>(IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration, RegistrationLifetime lifetime)
        {
            switch (lifetime)
            {
                case RegistrationLifetime.Scoped:
                    registration.InstancePerLifetimeScope();
                    break;
                case RegistrationLifetime.Singleton:
                    registration.SingleInstance();
                    break;
            }
        }

        #endregion

    }
}
