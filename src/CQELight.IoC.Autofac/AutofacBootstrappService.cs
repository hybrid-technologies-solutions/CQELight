using System;
using System.Composition;

namespace CQELight.IoC.Autofac
{
    [Export(typeof(IBootstrapperService))]
    internal class AutofacBootstrappService : IBootstrapperService
    {
        #region IBootstrapperService

        public BootstrapperServiceType ServiceType => BootstrapperServiceType.IoC;

        public Action<BootstrappingContext> BootstrappAction { get; internal set; }
           = (ctx) => BootstrapperExt.ConfigureAutofacContainer(ctx.Bootstrapper, _ => { }, new string[0]);

        #endregion
    }
}
