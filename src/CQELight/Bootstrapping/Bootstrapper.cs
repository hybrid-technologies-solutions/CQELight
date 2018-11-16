﻿using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Bootstrapping.Notifications;
using CQELight.Dispatcher;
using CQELight.Dispatcher.Configuration;
using CQELight.IoC;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using System;
using System.Collections.Generic;
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

        private readonly List<ITypeRegistration> _iocRegistrations;
        private readonly bool _strict;
        private readonly List<IBootstrapperService> _services;
        private readonly bool _checkOptimal;

        #endregion

        #region Properties

        /// <summary>
        /// Collection of components registration.
        /// </summary>
        public IEnumerable<ITypeRegistration> IoCRegistrations => _iocRegistrations.AsEnumerable();

        /// <summary>
        /// Collection of registered services.
        /// </summary>
        public IEnumerable<IBootstrapperService> RegisteredServices => _services.AsEnumerable();

        #endregion

        #region Ctor

        /// <summary>
        /// Create a new bootstrapper for the application.
        /// </summary>
        /// <param name="strict">Flag to indicates if bootstrapper should stricly validates its content.</param>
        /// <param name="checkOptimal">Flag to indicates if optimal system is currently 'On', which means
        /// that one service of each kind should be provided.</param>
        public Bootstrapper(bool strict = false, bool checkOptimal = false)
        {
            _services = new List<IBootstrapperService>();
            _iocRegistrations = new List<ITypeRegistration>();
            _strict = strict;
            _checkOptimal = checkOptimal;

            AddIoCRegistration(new TypeRegistration(typeof(BaseDispatcher), typeof(IDispatcher)));
        }

        #endregion

        #region Public methods 

        /// <summary>
        /// Add a global filter for all DLLs to exclude when calling GetAllTypes methods.
        /// </summary>
        /// <param name="dllsNames">Name (without extension and path, case sensitive) of DLLs to globally exclude.</param>
        /// <returns>Bootstrapper instance.</returns>
        public Bootstrapper GloballyExcludeDLLsForTypeSearching(IEnumerable<string> dllsNames)
        {
            ReflectionTools._globallyExcludedDlls = dllsNames ?? throw new ArgumentNullException(nameof(dllsNames));
            return this;
        }

        /// <summary>
        /// Perform the bootstrapping of all configured services.
        /// </summary>
        public List<BootstrapperNotification> Bootstrapp()
        {
            var notifications = new List<BootstrapperNotification>();
            if (_checkOptimal)
            {
                CheckIfOptimal(notifications);
            }
            if (_services.Any(s => s.ServiceType == BootstrapperServiceType.IoC))
            {
                AddDispatcherToIoC();
            }
            var context = new BootstrappingContext(
                        _services.Select(s => s.ServiceType).Distinct(),
                        _iocRegistrations.SelectMany(r => r.AbstractionTypes)
                    );
            foreach (var service in _services.OrderByDescending(s => s.ServiceType))
            {
                service.BootstrappAction.Invoke(context);
            }
            return notifications;
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

            if (_strict && service.ServiceType != BootstrapperServiceType.Bus)
            {
                var currentService = _services.Find(s => s.ServiceType == service.ServiceType);
                if (currentService != null)
                {
                    throw new InvalidOperationException($"Bootstrapper.AddService() : A service of type {service.ServiceType} has already been added." +
                        $"Current registered service : {currentService.GetType().FullName}");
                }
            }
            _services.Add(service);
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
            _iocRegistrations.Add(new InstanceTypeRegistration(dispatcherConfiguration, typeof(DispatcherConfiguration)));
            return this;
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
            _iocRegistrations.Add(registration);
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

        #endregion

        #region Private methods

        private void AddDispatcherToIoC()
        {
            if (!_iocRegistrations.SelectMany(r => r.AbstractionTypes).Any(t => t == typeof(IDispatcher)))
            {
                _iocRegistrations.Add(new TypeRegistration(typeof(BaseDispatcher), typeof(IDispatcher), typeof(BaseDispatcher)));
            }
            if (!_iocRegistrations.SelectMany(r => r.AbstractionTypes).Any(t => t == typeof(DispatcherConfiguration)))
            {
                _iocRegistrations.Add(new InstanceTypeRegistration(DispatcherConfiguration.Default, typeof(DispatcherConfiguration)));
            }
        }

        private void CheckIfOptimal(List<BootstrapperNotification> notifications)
        {
            if (!_services.Any(s => s.ServiceType == BootstrapperServiceType.Bus))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.BusServiceMissing });
            }
            if (!_services.Any(s => s.ServiceType == BootstrapperServiceType.DAL))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.DALServiceMissing });
            }
            if (!_services.Any(s => s.ServiceType == BootstrapperServiceType.EventStore))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.EventStoreServiceMissing });
            }
            if (!_services.Any(s => s.ServiceType == BootstrapperServiceType.IoC))
            {
                notifications.Add(new BootstrapperNotification { Type = BootstrapperNotificationType.Warning, ContentType = BootstapperNotificationContentType.IoCServiceMissing });
            }
        }


        #endregion

    }
}
