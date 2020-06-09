using CQELight;
using CQELight.IoC;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHelloWorld
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            new Bootstrapper()
                .UseInMemoryCommandBus()
                .UseInMemoryEventBus()
                .UseAutofacAsIoC()
                .Bootstrapp();

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var scope = DIManager.BeginScope();
            scope.Resolve<MainWindow>().Show();
        }
    }
}
