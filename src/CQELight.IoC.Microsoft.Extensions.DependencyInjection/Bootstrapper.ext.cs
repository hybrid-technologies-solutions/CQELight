using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Bootstrapping.Notifications;
using CQELight.IoC;
using CQELight.IoC.Microsoft.Extensions.DependencyInjection;
using CQELight.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight
{
    public static class BootstrapperExtensions
    {
        #region Public static methods

        public static Bootstrapper UseMicrosoftDependencyInjection(this Bootstrapper bootstrapper,
            IServiceCollection services, params string[] excludedDllsForAutoRegistration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var service = new MicrosoftDependencyInjectionService
            {
                BootstrappAction = (ctx) =>
                {
                    AddComponentRegistrationToContainer(services, bootstrapper.IoCRegistrations);
                    AddAutoRegisteredTypes(bootstrapper, services, excludedDllsForAutoRegistration);
                    DIManager.Init(new MicrosoftScopeFactory(services));
                }
            };

            bootstrapper.AddService(service);
            return bootstrapper;
        }

        #endregion

        #region Private static methods

        private static void AddAutoRegisteredTypes(Bootstrapper bootstrapper, IServiceCollection services, string[] excludedDllsForAutoRegistration)
        {
            bool CheckPublicConstructorAvailability(Type type)
            {
                if (!type.GetConstructors().Any(c => c.IsPublic))
                {
                    bootstrapper.AddNotification(new BootstrapperNotification(BootstrapperNotificationType.Error, "You must provide public constructor to Microsoft.Extensions.DependencyInjection extension cause it only supports public constructor. If you want to use internal or private constructor, switch to another IoC provider that supports this feature."));
                    return false;
                }
                return true;
            }

            foreach (var type in ReflectionTools.GetAllTypes(excludedDllsForAutoRegistration)
                .Where(t => typeof(IAutoRegisterType).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList())
            {
                if (CheckPublicConstructorAvailability(type))
                {
                    services.AddTransient(type, type);
                    foreach (var @interface in type.GetInterfaces())
                    {
                        services.AddTransient(@interface, type);
                    }
                }
            }

            foreach (var type in ReflectionTools.GetAllTypes(excludedDllsForAutoRegistration)
                .Where(t => typeof(IAutoRegisterTypeSingleInstance).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList())
            {
                if (CheckPublicConstructorAvailability(type))
                {
                    services.AddSingleton(type, type);
                    foreach (var @interface in type.GetInterfaces())
                    {
                        services.AddSingleton(@interface, type);
                    }
                }
            }
        }

        private static void AddComponentRegistrationToContainer(IServiceCollection services, IEnumerable<ITypeRegistration> customRegistration)
        {
            if (customRegistration?.Any() == false)
            {
                return;
            }
            foreach (var item in customRegistration)
            {
                if (item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(TypeRegistration<>))
                {
                    var instanceTypeValue = item.GetType().GetProperty("InstanceType").GetValue(item) as Type;
                    var abstractionTypes = (item.GetType().GetProperty("AbstractionTypes").GetValue(item) as IEnumerable<Type>).ToArray();
                    var lifeTime = (RegistrationLifetime)item.GetType().GetProperty("Lifetime").GetValue(item);
                    switch (lifeTime)
                    {
                        case RegistrationLifetime.Scoped:
                            services.AddScoped(instanceTypeValue, instanceTypeValue);
                            break;
                        case RegistrationLifetime.Singleton:
                            services.AddSingleton(instanceTypeValue, instanceTypeValue);
                            break;
                        case RegistrationLifetime.Transient:
                            services.AddTransient(instanceTypeValue, instanceTypeValue);
                            break;
                    }
                    foreach (var abstractionType in abstractionTypes.Where(t => t != instanceTypeValue))
                    {
                        switch (lifeTime)
                        {
                            case RegistrationLifetime.Scoped:
                                services.AddScoped(abstractionType, instanceTypeValue);
                                break;
                            case RegistrationLifetime.Singleton:
                                services.AddSingleton(abstractionType, instanceTypeValue);
                                break;
                            case RegistrationLifetime.Transient:
                                services.AddTransient(abstractionType, instanceTypeValue);
                                break;
                        }
                    }
                }
                else if (item is InstanceTypeRegistration instanceTypeRegistration)
                {
                    foreach (var type in item.AbstractionTypes)
                    {
                        switch (instanceTypeRegistration.Lifetime)
                        {
                            case RegistrationLifetime.Scoped:
                                services.AddScoped(type, _ => instanceTypeRegistration.Value);
                                break;
                            case RegistrationLifetime.Singleton:
                                services.AddSingleton(type, _ => instanceTypeRegistration.Value);
                                break;
                            case RegistrationLifetime.Transient:
                                services.AddTransient(type, _ => instanceTypeRegistration.Value);
                                break;
                        }
                    }
                }
                else if (item is TypeRegistration typeRegistration)
                {
                    foreach (var type in item.AbstractionTypes)
                    {
                        switch (typeRegistration.Lifetime)
                        {
                            case RegistrationLifetime.Scoped:
                                services.AddScoped(type, typeRegistration.InstanceType);
                                break;
                            case RegistrationLifetime.Singleton:
                                services.AddSingleton(type, typeRegistration.InstanceType);
                                break;
                            case RegistrationLifetime.Transient:
                                services.AddTransient(type, typeRegistration.InstanceType);
                                break;
                        }
                    }
                }
                else if (item is FactoryRegistration factoryRegistration)
                {
                    object AddFactoryRegistration(IServiceProvider serviceProvider)
                    {
                        if (factoryRegistration.Factory != null)
                        {
                           return factoryRegistration.Factory();
                        }
                        else if (factoryRegistration.ScopedFactory != null)
                        {
                            return factoryRegistration.ScopedFactory(new MicrosoftScope(serviceProvider.CreateScope(), services));
                        }
                        throw new InvalidOperationException("FactoryRegistration has not been correctly configured (both Factory and ScopedFactory are null).");
                    }
                    foreach (var type in item.AbstractionTypes)
                    {
                        switch (factoryRegistration.Lifetime)
                        {
                            case RegistrationLifetime.Scoped:
                                services.AddScoped(type, serviceProvider => AddFactoryRegistration(serviceProvider));
                                break;
                            case RegistrationLifetime.Singleton:
                                services.AddSingleton(type, serviceProvider => AddFactoryRegistration(serviceProvider));
                                break;
                            case RegistrationLifetime.Transient:
                                services.AddTransient(type, serviceProvider => AddFactoryRegistration(serviceProvider));
                                break;
                        }
                    }
                }
            }
        }


        #endregion

    }
}
