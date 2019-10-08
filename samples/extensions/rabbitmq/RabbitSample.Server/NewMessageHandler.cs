using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using RabbitSample.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitSample.Server
{
    public class NewMessageHandler : IDomainEventHandler<NewMessage>
    {
        public Task<Result> HandleAsync(NewMessage domainEvent, IEventContext context = null)
        {
            Console.WriteLine($"Received : {domainEvent.Payload}");
            return Result.Ok();
        }
    }
}
