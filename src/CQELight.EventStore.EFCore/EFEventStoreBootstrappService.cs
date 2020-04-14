using System;

namespace CQELight
{
    internal class EFEventStoreBootstrappService : IBootstrapperService
    {
        public BootstrapperServiceType ServiceType => BootstrapperServiceType.EventStore;
        public Action<BootstrappingContext> BootstrappAction { get; }

        public EFEventStoreBootstrappService(Action<BootstrappingContext> bootstrappAction)
        {
            BootstrappAction = bootstrappAction ?? throw new ArgumentNullException(nameof(bootstrappAction));
        }
    }
}
