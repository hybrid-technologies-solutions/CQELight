using Autofac;
using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC.Attributes;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using System.Linq;
using System.Reflection;
using A = Autofac;

namespace CQELight.IoC.Autofac
{
    internal class AutoRegisterModule : A.Module
    {
        #region Members

        private readonly string[] excludedAutoRegisterTypeDlls;

        #endregion

        #region Ctor

        public AutoRegisterModule(params string[] excludedAutoRegisterTypeDlls)
        {
            this.excludedAutoRegisterTypeDlls = (excludedAutoRegisterTypeDlls ?? Enumerable.Empty<string>()).Concat(new[] { "Autofac" }).ToArray();
        }

        #endregion

        #region Override methods
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            foreach (var type in ReflectionTools.GetAllTypes(excludedAutoRegisterTypeDlls)
                .Where(t =>
                    (t.ImplementsRawGenericInterface(typeof(ICommandHandler<>)) || t.ImplementsRawGenericInterface(typeof(IDomainEventHandler<>))) && t.IsClass && !t.IsAbstract).ToList())
            {
                var registration = builder.RegisterType(type)
                    .IfNotRegistered(type)
                    .AsImplementedInterfaces()
                    .AsSelf();
                if (!type.IsDefined(typeof(DefineTypeResolutionModeAttribute))
                    || type.GetCustomAttribute<DefineTypeResolutionModeAttribute>()?.Mode == TypeResolutionMode.Full)
                {
                    registration.FindConstructorsWith(new FullConstructorFinder());
                }
            }

            foreach (var type in ReflectionTools.GetAllTypes(excludedAutoRegisterTypeDlls)
                .Where(t => typeof(IAutoRegisterType).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList())
            {
                var registration = builder.RegisterType(type)
                    .IfNotRegistered(type)
                    .AsImplementedInterfaces()
                    .AsSelf();
                if (!type.IsDefined(typeof(DefineTypeResolutionModeAttribute))
                    || type.GetCustomAttribute<DefineTypeResolutionModeAttribute>()?.Mode == TypeResolutionMode.Full)
                {
                    registration.FindConstructorsWith(new FullConstructorFinder());
                }
            }

            foreach (var type in ReflectionTools.GetAllTypes(excludedAutoRegisterTypeDlls)
                .Where(t => typeof(IAutoRegisterTypeSingleInstance).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList())
            {
                var registration = builder.RegisterType(type)
                    .IfNotRegistered(type)
                    .AsImplementedInterfaces()
                    .AsSelf()
                    .SingleInstance();
                if (!type.IsDefined(typeof(DefineTypeResolutionModeAttribute))
                    || type.GetCustomAttribute<DefineTypeResolutionModeAttribute>()?.Mode == TypeResolutionMode.Full)
                {
                    registration.FindConstructorsWith(new FullConstructorFinder());
                }
            }
        }

        #endregion

    }
}
