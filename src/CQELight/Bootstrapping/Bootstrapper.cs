using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Bootstrapping;
using CQELight.Bootstrapping.Notifications;
using CQELight.DAL;
using CQELight.DAL.Interfaces;
using CQELight.Dispatcher;
using CQELight.Dispatcher.Configuration;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Text;

namespace CQELight
{
    /// <summary>
    /// Bootstrapping class for system initialisation.
    /// </summary>
    public class Bootstrapper
    {
        #region Members

        private readonly List<ITypeRegistration> iocRegistrations;
        private readonly bool strict;
        private readonly List<IBootstrapperService> services;
        private readonly bool checkOptimal;
        private readonly List<BootstrapperNotification> notifications;
        private readonly bool throwExceptionOnErrorNotif;
        private readonly bool useMef;

        #endregion

        #region Properties

        /// <summary>
        /// Collection of components registration.
        /// </summary>
        public IEnumerable<ITypeRegistration> IoCRegistrations => iocRegistrations.AsEnumerable();

        /// <summary>
        /// Collection of registered services.
        /// </summary>
        public IEnumerable<IBootstrapperService> RegisteredServices => services.AsEnumerable();

        /// <summary>
        /// MEF auto wired up services.
        /// </summary>
        [Import]
        private IEnumerable<IBootstrapperService> MEFServices { get; set; }

        #endregion

        #region Event

        /// <summary>
        /// Occurs when bootstrapping is finished.
        /// </summary>
        public event Action<PostBootstrappingContext> OnPostBootstrapping;

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new bootstrapper for the application,
        /// which has no strict verifications nor optimal verifications.
        /// </summary>
        public Bootstrapper()
        {
            notifications = new List<BootstrapperNotification>();
            services = new List<IBootstrapperService>();
            iocRegistrations = new List<ITypeRegistration>();
        }

        /// <summary>
        /// Create a new strict bootstrapper.
        /// Strict implies that content is validate and exception are thrown if
        /// something is not good in configuration.
        /// </summary>
        /// <param name="strict">Flag to indicates if bootstrapper should stricly validates its content.</param>

        [Obsolete("Use Bootstrapper(BootstrapperOptions) instead. This ctor will be removed in 2.0")]
        public Bootstrapper(bool strict)
            : this()
        {
            this.strict = strict;
        }

        /// <summary>
        /// Create a new bootstrapper for the application.
        /// </summary>
        /// <param name="strict">Flag to indicates if bootstrapper should stricly validates its content.</param>
        /// <param name="checkOptimal">Flag to indicates if optimal system is currently 'On', which means
        /// that one service of each kind should be provided.</param>

        [Obsolete("Use Bootstrapper(BootstrapperOptions) instead. This ctor will be removed in 2.0")]
        public Bootstrapper(bool strict, bool checkOptimal)
            : this(strict)
        {
            this.checkOptimal = checkOptimal;
        }

        /// <summary>
        /// Create a new bootstrapper for the application with the following parameters.
        /// </summary>
        /// <param name="strict">Flag to indicates if bootstrapper should stricly validates its content.</param>
        /// <param name="checkOptimal">Flag to indicates if optimal system is currently 'On', which means
        /// that one service of each kind should be provided.</param>
        /// <param name="throwExceptionOnErrorNotif">Flag to indicates if any encountered error notif
        /// should throw <see cref="BootstrappingException"/></param>

        [Obsolete("Use Bootstrapper(BootstrapperOptions) instead. This ctor will be removed in 2.0")]
        public Bootstrapper(bool strict, bool checkOptimal, bool throwExceptionOnErrorNotif)
            : this(strict, checkOptimal)
        {
            this.throwExceptionOnErrorNotif = throwExceptionOnErrorNotif;
        }

        /// <summary>
        /// Create a new instance of Boostrapper with defined options.
        /// </summary>
        /// <param name="options">Options to use.</param>
        public Bootstrapper(BootstrapperOptions options)
            : this()
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            useMef = options.AutoLoad;
            strict = options.Strict;
            throwExceptionOnErrorNotif = options.ThrowExceptionOnErrorNotif;
            checkOptimal = options.CheckOptimal;
        }

        #endregion

        #region Public methods 

        /// <summary>
        /// Add a global static filter for all DLLs to exclude when calling GetAllTypes methods.
        /// </summary>
        /// <param name="dllsNames">Name (without extension and path, case sensitive) of DLLs to globally exclude.</param>
        /// <returns>Bootstrapper instance.</returns>
        public Bootstrapper GloballyExcludeDLLsForTypeSearching(IEnumerable<string> dllsNames)
        {
            ReflectionTools.s_DLLBlackList = dllsNames ?? throw new ArgumentNullException(nameof(dllsNames));
            ReflectionTools.s_DLLsWhiteList = Enumerable.Empty<string>();
            return this;
        }

        /// <summary>
        /// Add a global filter for all methods that use DLLs for searching to only allows
        /// those who are presents in this methods.
        /// Note : this is globally exclusive with GloballyExcludeDLLsForTypeSearching
        /// </summary>
        /// <param name="dllsNames">DLLs name to include in searching</param>
        /// <returns>Bootstrapper instance</returns>
        public Bootstrapper OnlyIncludeDLLsForTypeSearching(params string[] dllsNames)
        {
            ReflectionTools.s_DLLBlackList = Enumerable.Empty<string>();
            ReflectionTools.s_DLLsWhiteList = dllsNames;
            return this;
        }

        /// <summary>
        /// Perform the bootstrapping of all configured services.
        /// </summary>
        /// <returns>Collection of generated notifications.</returns>
        public IEnumerable<BootstrapperNotification> Bootstrapp()
        {
            if (useMef)
            {
                using (var container = new ContainerConfiguration()
                    .WithParts(ReflectionTools.GetAllTypes())
                    .CreateContainer())
                {
                    MEFServices = container.GetExports<IBootstrapperService>();
                }
            }
            BootstrappServices(useMef ? MEFServices : RegisteredServices);
            if (throwExceptionOnErrorNotif && notifications.Any(n => n.Type == BootstrapperNotificationType.Error))
            {
                throw new BootstrappingException(notifications);
            }
            if (OnPostBootstrapping != null)
            {
                OnPostBootstrapping(new PostBootstrappingContext
                {
                    Notifications = notifications,
                    Scope = DIManager.IsInit ? DIManager.BeginScope() : null
                });
                OnPostBootstrapping = null; //Unsubscribe all
            }
            return notifications.AsEnumerable();
        }

        /// <summary>
        /// Add a service to the collection of bootstrapped services.
        /// </summary>
        /// <param name="service">Service.</param>
        public Bootstrapper AddService(IBootstrapperService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (strict && !service.ServiceType.In(BootstrapperServiceType.Bus, BootstrapperServiceType.Other))
            {
                var currentService = services.Find(s => s.ServiceType == service.ServiceType);
                if (currentService != null)
                {
                    throw new InvalidOperationException($"Bootstrapper.AddService() : A service of type {service.ServiceType} has already been added." +
                        $"Current registered service : {currentService.GetType().FullName}");
                }
            }
            services.Add(service);
            return this;
        }

        /// <summary>
        /// Setting up system dispatching configuration with the following configuration.
        /// All manually created dispatchers will be created using their own configuration if speciffied,
        /// or the one specified here. If this method is not called, default configuration will be used for 
        /// all dispatchers.
        /// Configuration passed here will be applied to CoreDispatcher as well.
        /// </summary>
        /// <param name="dispatcherConfiguration">Configuration to use.</param>
        /// <returns>Instance of the boostraper</returns>
        public Bootstrapper ConfigureDispatcher(DispatcherConfiguration dispatcherConfiguration)
        {
            DispatcherConfiguration.Current = dispatcherConfiguration ?? throw new ArgumentNullException(nameof(dispatcherConfiguration));
            if (services.Any(s => s.ServiceType == BootstrapperServiceType.IoC))
            {
                iocRegistrations.Add(new InstanceTypeRegistration(dispatcherConfiguration, typeof(DispatcherConfiguration)));
            }
            return this;
        }

        /// <summary>
        /// Setting up system dispatching configuration with the following configuration.
        /// All manually created dispatchers will be created using their own configuration if speciffied,
        /// or the one specified here. If this method is not called, default configuration will be used for 
        /// all dispatchers.
        /// Configuration passed here will be applied to CoreDispatcher as well.
        /// There's no need to call "Build" at the end of this method.
        /// </summary>
        /// <param name="dispatcherConfigurationAction">Fluent configuration to apply.</param>
        /// <returns>Instance of the boostraper</returns>
        public Bootstrapper ConfigureDispatcher(Action<DispatcherConfigurationBuilder> dispatcherConfigurationAction)
        {
            if (dispatcherConfigurationAction == null)
            {
                throw new ArgumentNullException(nameof(dispatcherConfigurationAction));
            }

            var builder = new DispatcherConfigurationBuilder();
            dispatcherConfigurationAction(builder);

            var configuration = builder.Build();
            return ConfigureDispatcher(configuration);
        }

        /// <summary>
        /// Add a custom component IoC registration into Bootstrapper for IoC component.
        /// </summary>
        /// <param name="registration">Registration to add.</param>
        /// <returns>Instance of the boostrapper</returns>
        public Bootstrapper AddIoCRegistration(ITypeRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }
            iocRegistrations.Add(registration);
            return this;
        }

        /// <summary>
        /// Add some registrations to the bootrapper for the IoC component
        /// </summary>
        /// <param name="registrations">Collection of registrations.</param>
        /// <returns>Instance of the bootstrapper.</returns>
        public Bootstrapper AddIoCRegistrations(params ITypeRegistration[] registrations)
        {
            if (registrations?.Any() == true)
            {
                registrations.DoForEach(r => AddIoCRegistration(r));
            }
            return this;
        }

        /// <summary>
        /// Add a notification within the bootstrapper that will be 
        /// returned when bootstrapping.
        /// </summary>
        /// <param name="notification">Notification to add.</param>
        public void AddNotification(BootstrapperNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }
            notifications.Add(notification);
        }

        /// <summary>
        /// Add a range of notifications within the bootstrapper that will be
        /// returned when bootstrapping.
        /// </summary>
        /// <param name="notifications">Collection of notifications to add.</param>
        public void AddNotifications(IEnumerable<BootstrapperNotification> notifications)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException(nameof(notifications));
            }
            this.notifications.AddRange(notifications);
        }

        #endregion

        #region Private methods

        private void BootstrappServices(IEnumerable<IBootstrapperService> services)
        {
            var iocServiceExists = services.Any(s => s.ServiceType == BootstrapperServiceType.IoC);
            if (checkOptimal)
            {
                CheckIfOptimal();
            }
            if (iocRegistrations.Count > 0 && !iocServiceExists)
            {
                if (strict)
                {
                    throw new InvalidOperationException("Bootstsrapper.Bootstrapp() : Some IoC registrations " +
                        "has been made but no IoC has been registered. System cannot work.");
                }
                else
                {
                    notifications.Add(new BootstrapperNotification
                    {
                        Type = BootstrapperNotificationType.Error,
                        ContentType = BootstapperNotificationContentType.IoCRegistrationsHasBeenMadeButNoIoCService
                    });
                }
            }
            if (iocServiceExists)
            {
                AddDispatcherToIoC();
                AddToolboxToIoC();
                AddRepositoriesToIoC();
            }
            var context = new BootstrappingContext(
                        services.Select(s => s.ServiceType).Distinct(),
                        iocRegistrations.SelectMany(r => r.AbstractionTypes)
                    )
            {
                CheckOptimal = checkOptimal,
                Strict = strict,
                Bootstrapper = this
            };
            foreach (var service in services.OrderByDescending(s => s.ServiceType))
            {
                service.BootstrappAction.Invoke(context);
            }
        }

        private void AddRepositoriesToIoC()
        {
            if (!iocRegistrations.SelectMany(r => r.AbstractionTypes).Any(t =>
            t.In(typeof(IDatabaseRepository), typeof(IDatabaseRepository), typeof(IDataUpdateRepository))))
            {
                iocRegistrations.Add(new TypeRegistration<RepositoryBase>(true));
            }
        }

        private void AddDispatcherToIoC()
        {
            if (!iocRegistrations.SelectMany(r => r.AbstractionTypes).Any(t => t == typeof(IDispatcher)))
            {
                iocRegistrations.Add(new TypeRegistration(typeof(BaseDispatcher), typeof(IDispatcher), typeof(BaseDispatcher)));
            }
            if (!iocRegistrations.SelectMany(r => r.AbstractionTypes).Any(t => t == typeof(DispatcherConfiguration)))
            {
                iocRegistrations.Add(new InstanceTypeRegistration(DispatcherConfiguration.Default, typeof(DispatcherConfiguration)));
            }
        }

        private void CheckIfOptimal()
        {
            if (!services.Any(s => s.ServiceType == BootstrapperServiceType.Bus))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.BusServiceMissing });
            }
            if (!services.Any(s => s.ServiceType == BootstrapperServiceType.DAL))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.DALServiceMissing });
            }
            if (!services.Any(s => s.ServiceType == BootstrapperServiceType.EventStore))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.EventStoreServiceMissing });
            }
            if (!services.Any(s => s.ServiceType == BootstrapperServiceType.IoC))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.IoCServiceMissing });
            }
        }


        private void AddToolboxToIoC()
        {
            iocRegistrations.Add(new TypeRegistration<CQELightToolbox>(true));
        }

        #endregion

    }
}
