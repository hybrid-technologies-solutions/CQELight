using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using PizzaApp.Events;
using PizzaApp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaApp.Handlers.Events
{
    public class PizzasLoadedHandler_Persistence : IDomainEventHandler<PizzasLoaded>, IAutoRegisterType
    {
        private readonly IPizzaStorage storage;

        public PizzasLoadedHandler_Persistence(IPizzaStorage storage)
        {
            this.storage = storage;
        }

        public async Task<Result> HandleAsync(PizzasLoaded domainEvent, IEventContext context = null)
        {
            if (domainEvent.Pizzas.Any())
            {
                await storage.SavePizzasAsync(domainEvent.Pizzas);
            }
            return Result.Ok();
        }
    }
}
