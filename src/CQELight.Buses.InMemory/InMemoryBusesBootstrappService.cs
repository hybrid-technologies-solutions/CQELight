using CQELight.Buses.InMemory.Commands;
using CQELight.Buses.InMemory.Events;
using System;
using System.Composition;

namespace CQELight.Buses.InMemory
{
    [Export(typeof(IBootstrapperService))]
    internal class InMemoryBusesBootstrappService : IBootstrapperService
    {
        #region Static members

        private static readonly object s_threadSafety = new object();

        private static InMemoryBusesBootstrappService? _instance;

        internal static InMemoryBusesBootstrappService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (s_threadSafety)
                    {
                        if (_instance == null)
                        {
                            _instance = new InMemoryBusesBootstrappService();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region IBootstrapperService

        public BootstrapperServiceType ServiceType => BootstrapperServiceType.Bus;

        public Action<BootstrappingContext> BootstrappAction { get; internal set; } = (ctx) =>
        {
            BootstrapperExt.ConfigureInMemoryEventBus(ctx.Bootstrapper, InMemoryEventBusConfiguration.Default, new string[0], ctx);
            BootstrapperExt.ConfigureInMemoryCommandBus(ctx.Bootstrapper, InMemoryCommandBusConfiguration.Default, new string[0], ctx);
        };

        #endregion

    }
}
