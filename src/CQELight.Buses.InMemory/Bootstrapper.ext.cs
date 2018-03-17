﻿using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using CQELight.Dispatcher;
using CQELight.IoC;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.InMemory
{
    public static class BootstrapperExt
    {

        /// <summary>
        /// Configure the bootstrapper to use InMemory buses for dispatching events.
        /// </summary>
        /// <param name="bootstrapper">Instance of boostrapper.</param>
        /// <param name="configuration">Configuration to use for in memory event bus.</param>
        public static Bootstrapper UseInMemoryEventBus(this Bootstrapper bootstrapper, InMemoryEventBusConfiguration configuration)
        {
            CoreDispatcher.ConfigureBus<InMemoryEventBus, InMemoryEventBusConfiguration>(configuration);
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(InMemoryEventBus), typeof(IDomainEventBus), typeof(InMemoryEventBus)));
            return bootstrapper;
        }
        /// <summary>
        /// Configure the bootstrapper to use InMemory buses for dispatching commands.
        /// </summary>
        /// <param name="bootstrapper">Instance of boostrapper.</param>
        public static Bootstrapper UseInMemoryCommandBus(this Bootstrapper bootstrapper)
        {
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(InMemoryCommandBus), typeof(ICommandBus), typeof(InMemoryCommandBus)));
            return bootstrapper;
        }

    }
}