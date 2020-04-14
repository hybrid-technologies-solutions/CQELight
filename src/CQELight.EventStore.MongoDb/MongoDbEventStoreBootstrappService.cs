using System;

namespace CQELight.EventStore.MongoDb
{
    internal class MongoDbEventStoreBootstrappService : IBootstrapperService
    {
        #region IBootstrapperService

        public BootstrapperServiceType ServiceType => BootstrapperServiceType.EventStore;

        public Action<BootstrappingContext> BootstrappAction { get; }

        #endregion

        #region Ctor

        public MongoDbEventStoreBootstrappService(Action<BootstrappingContext> bootstrappAction)
        {
            BootstrappAction = bootstrappAction ?? throw new ArgumentNullException(nameof(bootstrappAction));
        }

        #endregion
    }
}
