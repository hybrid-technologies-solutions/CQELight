using CQELight;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PizzaApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            new Bootstrapper()
                .UseAutofacAsIoC()
                .UseInMemoryCommandBus()
                .UseInMemoryEventBus()
                .Bootstrapp();

            var navigationPage = new NavigationPage(new MainPage());
            navigationPage.BarBackgroundColor = Color.FromHex("#1abbd4");
            MainPage = navigationPage;
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
