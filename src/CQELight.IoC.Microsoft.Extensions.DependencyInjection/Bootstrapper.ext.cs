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
            (_ =>
                {
                    AddComponentRegistrationToContainer(services, bootstrapper.IoCRegistrations.ToList());
                    AddAutoRegisteredTypes(bootstrapper, services, excludedDllsForAutoRegistration);
                    services.AddScoped<IScopeFactory>(s => new MicrosoftScopeFactory(s));
                    DIManager.Init(new MicrosoftScopeFactory(services));
                }
            );

            bootstrapper.AddService(service);
            return bootstrapper;
        }

        #endregion

        #region Private static methods

        private static void AddAutoRegisteredTypes(Bootstrapper bootstrapper, IServiceCollection services, string[] excludedDllsForAutoRegistration)
        {
            bool CheckPublicConstructorAvailability(Type type)
            {
                if (type.GetConstructors().Any(c => c.IsPublic))
                {
                    return true;
                }
                bootstrapper.AddNotification(new BootstrapperNotification(BootstrapperNotificationType.Error, "You must provide public constructor to Microsoft.Extensions.DependencyInjection extension cause it only supports public constructor. If you want to use internal or private constructor, switch to another IoC provider that supports this feature."));
                return false;
            }

            foreach (var type in ReflectionTools.GetAllTypes(excludedDllsForAutoRegistration)
                .Where(t => typeof(IAutoRegisterType).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList())
            {
                if (!CheckPublicConstructorAvailability(type))
                {
                    continue;
                }

                services.AddTransient(type, type);
                foreach (var @interface in type.GetInterfaces())
                {
                    services.AddTransient(@interface, type);
                }
            }

            foreach (var type in ReflectionTools.GetAllTypes(excludedDllsForAutoRegistration)
                .Where(t => typeof(IAutoRegisterTypeSingleInstance).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList())
            {
                if (!CheckPublicConstructorAvailability(type))
                {
                    continue;
                }

                services.AddSingleton(type, type);
                foreach (var @interface in type.GetInterfaces())
                {
                    services.AddSingleton(@interface, type);
                }
            }
        }

        private static void AddComponentRegistrationToContainer(IServiceCollection services, List<ITypeRegistration> customRegistration)
        {
            if (customRegistration == null || customRegistration.Count == 0)
            {
                return;
            }
            var alreadyExistingServices = new HashSet<Type>(services.Select(s => s.ServiceType));
            foreach (var item in customRegistration)
            {
                if (item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(TypeRegistration<>))
                {
                    var instanceTypeValue = item.GetType().GetProperty("InstanceType").GetValue(item) as Type;
                    var lifeTime = (RegistrationLifetime)item.GetType().GetProperty("Lifetime").GetValue(item);
                    var abstractionTypes = (item.GetType().GetProperty("AbstractionTypes").GetValue(item) as IEnumerable<Type>).ToArray();
                    if (instanceTypeValue != null && !alreadyExistingServices.Contains(instanceTypeValue))
                    {
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
                    }
                    foreach (var abstractionType in abstractionTypes.Where(t => t != instanceTypeValue))
                    {
                        if (instanceTypeValue != null && !alreadyExistingServices.Contains(abstractionType))
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

                }
                else
                {
                    switch (item)
                    {
                        case InstanceTypeRegistration instanceTypeRegistration:
                            {
                                foreach (var type in item.AbstractionTypes)
                                {
                                    if (!alreadyExistingServices.Contains(type))
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

                                break;
                            }

                        case TypeRegistration typeRegistration:
                            {
                                foreach (var type in item.AbstractionTypes)
                                {
                                    if (!alreadyExistingServices.Contains(type))
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

                                break;
                            }

                        case FactoryRegistration factoryRegistration:
                            {
                                object AddFactoryRegistration(IServiceProvider serviceProvider)
                                {
                                    if (factoryRegistration.Factory != null)
                                    {
                                        return factoryRegistration.Factory();
                                    }
                                    if (factoryRegistration.ScopedFactory != null)
                                    {
                                        return factoryRegistration.ScopedFactory(new MicrosoftScope(serviceProvider.CreateScope(), services));
                                    }
                                    throw new InvalidOperationException("FactoryRegistration has not been correctly configured (both Factory and ScopedFactory are null).");
                                }
                                foreach (var type in item.AbstractionTypes)
                                {
                                    if (!alreadyExistingServices.Contains(type))
                                    {
                                        switch (factoryRegistration.Lifetime)
                                        {
                                            case RegistrationLifetime.Scoped:
                                                services.AddScoped(type, AddFactoryRegistration);
                                                break;
                                            case RegistrationLifetime.Singleton:
                                                services.AddSingleton(type, AddFactoryRegistration);
                                                break;
                                            case RegistrationLifetime.Transient:
                                                services.AddTransient(type, AddFactoryRegistration);
                                                break;
                                        }
                                    }
                                }

                                break;
                            }
                    }
                }
            }
        }

        #endregion

    }
}
