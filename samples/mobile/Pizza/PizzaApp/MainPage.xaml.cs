using CQELight.IoC;
using CQELight.MVVM.Interfaces;
using CQELight.MVVM.XamarinForms;
using Newtonsoft.Json;
using PizzaApp.Models;
using PizzaApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PizzaApp
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPageCQELight
    {
        public MainPage()
        {
            InitializeComponent();
            using (var scope = DIManager.BeginScope())
            {
                BindingContext = scope.Resolve<MainPageViewModel>(new TypeResolverParameter(typeof(IView), this));
            }
        }
    }
}
