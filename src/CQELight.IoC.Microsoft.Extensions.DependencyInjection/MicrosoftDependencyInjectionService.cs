using System;

namespace CQELight.IoC.Microsoft.Extensions.DependencyInjection
{
    internal class MicrosoftDependencyInjectionService : IBootstrapperService
    {
        public BootstrapperServiceType ServiceType => BootstrapperServiceType.IoC;
        public Action<BootstrappingContext> BootstrappAction { get; }

        public MicrosoftDependencyInjectionService(Action<BootstrappingContext> bootstrappAction)
        {
            BootstrappAction = bootstrappAction ?? throw new ArgumentNullException(nameof(bootstrappAction));
        }
    }
}
