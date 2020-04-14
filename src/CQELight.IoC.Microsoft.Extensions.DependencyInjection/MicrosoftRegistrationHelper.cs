using CQELight.Implementations.IoC;
using CQELight.Tools.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CQELight.IoC.Microsoft.Extensions.DependencyInjection
{
    internal static class MicrosoftRegistrationHelper
    {
        public static IServiceCollection Clone(this IServiceCollection services)
        {
            var clonedCollection = new ServiceCollection();

            foreach (var item in services)
            {
                switch (item.Lifetime)
                {
                    case ServiceLifetime.Scoped:
                        {
                            if (item.ImplementationType != null)
                                clonedCollection.AddScoped(item.ServiceType, item.ImplementationType);
                            if (item.ImplementationFactory != null)
                                clonedCollection.AddScoped(item.ServiceType, item.ImplementationFactory);
                            break;
                        }

                    case ServiceLifetime.Singleton:
                        {
                            if (item.ImplementationType != null)
                                clonedCollection.AddSingleton(item.ServiceType, item.ImplementationType);
                            if (item.ImplementationFactory != null)
                                clonedCollection.AddSingleton(item.ServiceType, item.ImplementationFactory);
                            break;
                        }

                    default:
                        {
                            if (item.ImplementationType != null)
                                clonedCollection.AddTransient(item.ServiceType, item.ImplementationType);
                            if (item.ImplementationFactory != null)
                                clonedCollection.AddTransient(item.ServiceType, item.ImplementationFactory);
                            break;
                        }
                }
            }

            return clonedCollection;
        }

        public static void RegisterContextTypes(IServiceCollection services, TypeRegister typeRegister)
        {
            typeRegister.Objects.DoForEach(o =>
            {
                if (o == null)
                {
                    return;
                }

                var objType = o.GetType();
                services.AddScoped(objType, _ => o);
                foreach (var @interface in objType.GetInterfaces())
                {
                    services.AddScoped(@interface, _ => o);
                }
            });
            typeRegister.Types.DoForEach(t =>
            {
                if (t == null)
                {
                    return;
                }

                services.AddScoped(t, t);
                foreach (var @interface in t.GetInterfaces())
                {
                    services.AddScoped(@interface, t);
                }
            });
            typeRegister.ObjAsTypes.DoForEach(kvp =>
            {
                if (kvp.Key == null)
                {
                    return;
                }

                foreach (var item in kvp.Value)
                {
                    services.AddScoped(item, _ => kvp.Key);
                }
            });
            typeRegister.TypeAsTypes.DoForEach(kvp =>
            {
                if (kvp.Key == null)
                {
                    return;
                }

                foreach (var item in kvp.Value)
                {
                    services.AddScoped(item, _ => kvp.Key);
                }
            });
        }
    }
}
