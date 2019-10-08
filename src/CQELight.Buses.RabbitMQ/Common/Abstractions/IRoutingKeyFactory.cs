using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ.Common.Abstractions
{
    /// <summary>
    /// Contract interface for routing key factory implementation.
    /// </summary>
    public interface IRoutingKeyFactory
    {
        /// <summary>
        /// Retrieve a routing key for a specific type of event.
        /// </summary>
        /// <typeparam name="T">Type of event we want to retrieve routing key.</typeparam>
        /// <param name="instance">Instance of event we want to retrieve routing key.</param>
        /// <returns>Routing key</returns>
        string GetRoutingKeyForEvent(object @event);

        /// <summary>
        /// Retrieve a routing key for a specific type of command.
        /// </summary>
        /// <typeparam name="T">Type of command we want to retrieve routing key.</typeparam>
        /// <param name="instance">Instance of command we want to retrieve routing key.</param>
        /// <returns>Routing key</returns>
        string GetRoutingKeyForCommand(object command);
    }
}
