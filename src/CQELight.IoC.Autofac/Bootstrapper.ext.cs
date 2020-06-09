using Autofac;
using Autofac.Builder;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC;
using CQELight.IoC.Autofac;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight
{
    public static class BootstrapperExt
    {
        #region Public static methods

        /// <summary>
        /// Configure the bootstrapper to use Autofac as IoC.
        /// </summary>
        /// <param name="bootstrapper">Instance of bootstrapper.</param>
        /// <param name="containerBuilder">Autofac container builder that has been configured according to app..</param>
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
                BootstrappAction = (_) => CreateConfigWithContainer(bootstrapper, containerBuilder, excludedAutoRegisterTypeDLLs)
            };
            bootstrapper.AddService(service);
            return bootstrapper;
        }

        /// <summary>
        /// Configure the bootstrapper to use Autofac as IoC, without custom registrations.
        /// Only system and plugin registrations will be added.
        /// </summary>
        /// <param name="bootstrapper">Instance of bootstrapper.</param>
        /// <param name="excludedAutoRegisterTypeDLLs">DLLs name to exclude from auto-configuration into IoC
        /// (IAutoRegisterType will be ineffective).</param>
        public static Bootstrapper UseAutofacAsIoC(this Bootstrapper bootstrapper, params string[] excludedAutoRegisterTypeDLLs)
            => UseAutofacAsIoC(bootstrapper, _ => { }, excludedAutoRegisterTypeDLLs);

        /// <summary>
        /// Configure the bootstrapper to use Autofac as IoC.
        /// </summary>
        /// <param name="bootstrapper">Instance of bootstrapper.</param>
        /// <param name="containerBuilderConfiguration">Configuration to apply on freshly created container builder.</param>
        /// <param name="excludedAutoRegisterTypeDLLs">DLLs name to exclude from auto-configuration into IoC
        /// (IAutoRegisterType will be ineffective).</param>
        public static Bootstrapper UseAutofacAsIoC(this Bootstrapper bootstrapper, Action<ContainerBuilder> containerBuilderConfiguration,
            params string[] excludedAutoRegisterTypeDLLs)
        {
            var service = new AutofacBootstrappService
            {
                BootstrappAction = (_) => ConfigureAutofacContainer(bootstrapper, containerBuilderConfiguration, excludedAutoRegisterTypeDLLs)
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
        /// <param name="bootstrapper">Instance of bootstrapper.</param>
        /// <param name="scope">Scope instance</param>
        /// <param name="excludedAutoRegisterTypeDLLs">DLLs name to exclude from auto-configuration into IoC
        /// (IAutoRegisterType will be ineffective).</param>
        /// <returns>Configured bootstrapper</returns>
        public static Bootstrapper UseAutofacAsIoC(this Bootstrapper bootstrapper, ILifetimeScope scope,
            params string[] excludedAutoRegisterTypeDLLs)
        {
            var service = new AutofacBootstrappService
            {
                BootstrappAction = (_) =>
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
            var container = containerBuilder.Build();
            AutofacScopeFactory.AutofacContainer = container;
            InitDIManagerAndCreateScopeFactory(container);
        }

        private static void InitDIManagerAndCreateScopeFactory(ILifetimeScope scope)
        {
            var loggerFactory = scope.ResolveOptional<ILoggerFactory>();
            var factory = loggerFactory != null ? new AutofacScopeFactory(scope, loggerFactory) : new AutofacScopeFactory(scope);
            DIManager.Init(factory);
        }

        private static void AddRegistrationsToContainerBuilder(Bootstrapper bootstrapper, ContainerBuilder containerBuilder, string[] excludedAutoRegisterTypeDLLs)
        {
            containerBuilder.RegisterModule(new AutoRegisterModule(excludedAutoRegisterTypeDLLs));
            AddComponentRegistrationToContainer(containerBuilder, bootstrapper.IoCRegistrations.ToList());
            containerBuilder
                .Register(c =>
                {
                    var loggerFactory = c.ResolveOptional<ILoggerFactory>();
                    if (loggerFactory != null)
                    {
                        return new AutofacScopeFactory(AutofacScopeFactory.AutofacContainer!, loggerFactory);
                    }
                    return new AutofacScopeFactory(AutofacScopeFactory.AutofacContainer!);
                })
                .AsSelf()
                .AsImplementedInterfaces();
            containerBuilder
                .Register(_ => new AutofacScope(AutofacScopeFactory.AutofacContainer!))
                .As<IScope>();
        }

        private static void AddComponentRegistrationToContainer(ContainerBuilder containerBuilder, List<ITypeRegistration> customRegistration)
        {
            if (customRegistration == null || customRegistration.Count == 0)
            {
                return;
            }
            foreach (var item in customRegistration)
            {
                switch (item)
                {
                    case InstanceTypeRegistration instanceTypeRegistration:
                        AddLifetime(
                            containerBuilder
                                .Register(_ => instanceTypeRegistration.Value)
                                .As(instanceTypeRegistration.AbstractionTypes.ToArray()),
                            instanceTypeRegistration.Lifetime);
                        break;
                    case TypeRegistration typeRegistration:
                        {
                            foreach (var serviceType in typeRegistration.AbstractionTypes)
                            {
                                if (serviceType.IsGenericTypeDefinition)
                                {
                                    var registration = containerBuilder
                                        .RegisterGeneric(typeRegistration.InstanceType)
                                        .As(serviceType);
                                    if (typeRegistration.Mode == TypeResolutionMode.Full)
                                    {
                                        registration = registration.FindConstructorsWith(new FullConstructorFinder());
                                    }
                                    AddLifetime(registration, typeRegistration.Lifetime);
                                }
                                else
                                {
                                    var registration = containerBuilder
                                        .RegisterType(typeRegistration.InstanceType)
                                        .As(serviceType);
                                    if (typeRegistration.Mode == TypeResolutionMode.Full)
                                    {
                                        registration = registration.FindConstructorsWith(new FullConstructorFinder());
                                    }
                                    AddLifetime(registration, typeRegistration.Lifetime);
                                }
                            }
                            break;
                        }
                    case FactoryRegistration factoryRegistration:
                        AddLifetime(
                            containerBuilder
                                .Register(c =>
                                {
                                    if (factoryRegistration.Factory != null)
                                    {
                                        return factoryRegistration.Factory.Invoke();
                                    }
                                    else if (factoryRegistration.ScopedFactory != null)
                                    {
                                        return factoryRegistration.ScopedFactory.Invoke(new AutofacScope(c));
                                    }
                                    throw new InvalidOperationException("FactoryRegistration has not been correctly configured (both Factory and ScopedFactory are null).");
                                })
                                .As(factoryRegistration.AbstractionTypes.ToArray()),
                            factoryRegistration.Lifetime);
                        break;
                }
            }
        }

        private static void AddLifetime<TLimit, TActivatorData, TRegistrationStyle>(
            IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration,
            RegistrationLifetime lifetime)
        {
            switch (lifetime)
            {
                case RegistrationLifetime.Scoped:
                    registration.InstancePerLifetimeScope();
                    break;
                case RegistrationLifetime.Singleton:
                    registration.SingleInstance();
                    break;
                case RegistrationLifetime.Transient:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, $"Specified lifetime {lifetime} is unknown");
            }
        }

        #endregion

    }
}
