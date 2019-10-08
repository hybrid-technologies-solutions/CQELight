using CQELight.Buses.RabbitMQ.Subscriber;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ
{
    internal class RabbitMQBootstrappService : IBootstrapperService
    {
        #region Static members

        private static RabbitMQBootstrappService _instance;

        internal static RabbitMQBootstrappService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RabbitMQBootstrappService();
                }
                return _instance;
            }
        }

        internal static RabbitSubscriber RabbitSubscriber { get; set; }

        #endregion

        #region IBoostrapperService

        public BootstrapperServiceType ServiceType => BootstrapperServiceType.Bus;

        public Action<BootstrappingContext> BootstrappAction
        {
            get; internal set;
        }

        #endregion

    }
}
