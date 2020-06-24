using CQELight.Abstractions.IoC.Interfaces;
using Newtonsoft.Json;
using PizzaApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PizzaApp.Filenames;

namespace PizzaApp.Storage
{
    public class FilePizzaStorage : IPizzaStorage, IAutoRegisterTypeSingleInstance
    {
        public async Task<IEnumerable<Pizza>> GetPizzasAsync()
        {
            if(File.Exists(Pizzas_FileName))
            {
                return JsonConvert.DeserializeObject<List<Pizza>>(File.ReadAllText(Pizzas_FileName));
            }
            return Enumerable.Empty<Pizza>();
        }

        public Task SavePizzasAsync(IEnumerable<Pizza> pizzas)
        {
            File.WriteAllText(Pizzas_FileName, JsonConvert.SerializeObject(pizzas));
            return Task.CompletedTask;
        }
    }
}
