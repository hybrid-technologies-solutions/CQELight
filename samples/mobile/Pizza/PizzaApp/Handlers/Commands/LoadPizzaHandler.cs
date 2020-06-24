using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using Newtonsoft.Json;
using PizzaApp.Commands;
using PizzaApp.Events;
using PizzaApp.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PizzaApp.Handlers.Commands
{
    public class LoadPizzaHandler : ICommandHandler<LoadPizza>, IAutoRegisterType
    {
        private readonly IDispatcher dispatcher;

        public LoadPizzaHandler(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public Task<Result> HandleAsync(LoadPizza command, ICommandContext context = null)
        {
            using (var client = new WebClient())
            {
                client.DownloadStringCompleted += Client_DownloadStringCompleted;
                client.DownloadStringAsync(new Uri("https://drive.google.com/uc?export=download&id=1iafq5zOPqq3uFF_sHktrxajHkQbv3DXs"));
            }
            return Result.Ok();
        }

        private async void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(e.Result))
                {
                    var pizzas = JsonConvert.DeserializeObject<List<Pizza>>(e.Result);
                    await dispatcher.PublishEventAsync(new PizzasLoaded(pizzas));
                    return;
                }
            }
            catch
            {
                // TODO log must be done here
            }
            await dispatcher.PublishEventAsync(new PizzasNotLoaded());
        }
    }
}
