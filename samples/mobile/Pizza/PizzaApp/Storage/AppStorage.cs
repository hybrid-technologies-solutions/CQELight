using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PizzaApp.Storage
{
    public class AppStorage : IAppStorage, IAutoRegisterTypeSingleInstance
    {
        public T GetValue<T>(string key)
        {
            if (Application.Current.Properties.TryGetValue(key, out object result) && result is T resultAsT)
            {
                return resultAsT;
            }
            return default;
        }

        public Task StoreValueAsync(string key, object value)
        {
            Application.Current.Properties[key] = value;
            return Application.Current.SavePropertiesAsync();
        }
    }
}
