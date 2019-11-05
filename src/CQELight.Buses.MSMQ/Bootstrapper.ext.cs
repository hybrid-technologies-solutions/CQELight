using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.MSMQ;
using CQELight.Buses.MSMQ.Client;
using CQELight.IoC;
using System;
using System.Linq;

namespace CQELight
{
    public static class BootstrapperExtensions
    {
        #region Public static methods

        /// <summary>
        /// Use MSMQ as bus for messaging.
        /// </summary>
        /// <param name="bootstrapper">Bootstrapper instance.</param>
        /// <returns>Bootstrapper instance.</returns>
        [Obsolete("MSMQ extension is no more supported and will be removed in V2")]
        public static Bootstrapper UseMSMQClientBus(this Bootstrapper bootstrapper, MSMQClientBusConfiguration configuration)
        {
            var service = MSMQBootstrappService.Instance;

            service.BootstrappAction = (ctx) =>
             {
                 if (ctx.IsServiceRegistered(BootstrapperServiceType.IoC))
                 {
                     bootstrapper.AddIoCRegistrations(
                       new TypeRegistration(typeof(MSMQClientBus), typeof(IDomainEventBus)),
                       new InstanceTypeRegistration(configuration ?? MSMQClientBusConfiguration.Default,
                           typeof(MSMQClientBusConfiguration)));
                 }
             };

            if (!bootstrapper.RegisteredServices.Any(s => s == service))
            {
                bootstrapper.AddService(service);
            }

            return bootstrapper;
        }

        #endregion

    }
}
