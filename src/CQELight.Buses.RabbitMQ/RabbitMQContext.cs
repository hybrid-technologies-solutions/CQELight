using CQELight.Buses.RabbitMQ.Subscriber;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ
{
    internal static class RabbitMQContext
    {
        #region Static members

        internal static AbstractBaseConfiguration? Configuration { get; set; }

        internal static RabbitSubscriber? RabbitSubscriber { get; set; }

        #endregion
    }
}
