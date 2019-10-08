using Autofac;
using CQELight;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Dispatcher;
using CQELight.IoC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using RabbitSample.Common;
using System;

namespace RabbitSample.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Trying to connect to RabbitMQ instance @locahost with guest:guest");

            var loggerFactory = new LoggerFactory();

            var network = RabbitNetworkInfos.GetConfigurationFor("server", RabbitMQExchangeStrategy.SingleExchange);
            //This network will produce a client_queue queue, bound to cqelight_global_exchange
            new Bootstrapper()
                .UseRabbitMQ(
                    ConnectionInfosHelper.GetConnectionInfos("server"),
                    network,
                    cfg => cfg.DispatchInMemory = true
                )
                .UseInMemoryEventBus()
                .UseAutofacAsIoC(c => c.Register(_ => loggerFactory).AsImplementedInterfaces().ExternallyOwned())
                .Bootstrapp();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Successfuly connected to RabbitMQ");
            Console.ResetColor();
            Console.WriteLine("Listening... Press any key to exit");
            Console.ReadLine();
        }
    }
}
