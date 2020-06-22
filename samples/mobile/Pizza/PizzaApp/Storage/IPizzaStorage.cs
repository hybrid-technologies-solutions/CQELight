using PizzaApp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PizzaApp.Storage
{
    public interface IPizzaStorage
    {
        Task SavePizzasAsync(IEnumerable<Pizza> pizzas);
        Task<IEnumerable<Pizza>> GetPizzasAsync();
    }
}
