using CQELight.Abstractions.Events;
using PizzaApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaApp.Events
{
    public class PizzasLoaded : BaseDomainEvent
    {
        public IEnumerable<Pizza> Pizzas { get; }

        public PizzasLoaded(IEnumerable<Pizza> pizzas)
        {
            Pizzas = pizzas ?? throw new ArgumentNullException(nameof(pizzas));
        }
    }
}
