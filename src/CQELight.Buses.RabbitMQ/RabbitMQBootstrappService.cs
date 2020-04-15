using CQELight.Buses.RabbitMQ.Subscriber;
using System;

namespace CQELight.Buses.RabbitMQ
{
    internal class RabbitMQBootstrappService : IBootstrapperService
    {
        #region IBoostrapperService

        public BootstrapperServiceType ServiceType => BootstrapperServiceType.Bus;

        public Action<BootstrappingContext> BootstrappAction { get; }

        #endregion

        #region Ctor

        public RabbitMQBootstrappService(Action<BootstrappingContext> bootstrappAction)
        {
            BootstrappAction = bootstrappAction ?? throw new ArgumentNullException(nameof(bootstrappAction));
        }

        #endregion

    }
}
