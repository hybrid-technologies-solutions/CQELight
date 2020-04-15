using System;

namespace CQELight.DAL.EFCore
{
    internal class DALEFCoreBootstrappService : IBootstrapperService
    {
        #region IBoostrapperService

        public BootstrapperServiceType ServiceType => BootstrapperServiceType.DAL;

        public Action<BootstrappingContext> BootstrappAction { get; }

        #endregion

        #region Ctor

        public DALEFCoreBootstrappService(Action<BootstrappingContext> bootstrappAction)
        {
            BootstrappAction = bootstrappAction ?? throw new ArgumentNullException(nameof(bootstrappAction));
        }

        #endregion
    }
}
