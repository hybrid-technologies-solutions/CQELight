using CQELight;
using CQELight.Buses.RabbitMQ.Network;
using CQELight.Dispatcher;
using RabbitSample.Common;
using System;

namespace RabbitSample.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Trying to connect to RabbitMQ instance @locahost with guest:guest");

            var network = RabbitNetworkInfos.GetConfigurationFor("client", RabbitMQExchangeStrategy.SingleExchange);
            //This network will produce a client_queue queue, bound to cqelight_global_exchange
            new Bootstrapper()
                .UseRabbitMQ(
                    ConnectionInfosHelper.GetConnectionInfos("client"),
                    network
                )
                .Bootstrapp();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Successfuly connected to RabbitMQ");
            Console.ResetColor();
            Console.WriteLine("Enter your message and press enter to send to server, or enter 'quit' to exit process");
            var data = Console.ReadLine();
            while (data != "quit")
            {
                CoreDispatcher.PublishEventAsync(new NewMessage { Payload = data });
                data = Console.ReadLine();
            }
        }
    }
}
