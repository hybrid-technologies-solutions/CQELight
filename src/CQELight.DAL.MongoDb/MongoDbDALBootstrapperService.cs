using System;

namespace CQELight.DAL.MongoDb
{
    internal class MongoDbDALBootstrapperService : IBootstrapperService
    {
        public BootstrapperServiceType ServiceType => BootstrapperServiceType.DAL;

        public Action<BootstrappingContext> BootstrappAction { get; }

        public MongoDbDALBootstrapperService(Action<BootstrappingContext> bootstrappAction)
        {
            BootstrappAction = bootstrappAction ?? throw new ArgumentNullException(nameof(bootstrappAction));
        }
    }
}
