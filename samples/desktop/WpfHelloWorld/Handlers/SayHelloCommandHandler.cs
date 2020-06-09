using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Dispatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfHelloWorld.Commands;
using WpfHelloWorld.Events;

namespace WpfHelloWorld.Handlers
{
    public class SayHelloCommandHandler : ICommandHandler<SayHelloCommand>
    {
        private readonly IDispatcher dispatcher;

        public SayHelloCommandHandler(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public async Task<Result> HandleAsync(SayHelloCommand command, ICommandContext context = null)
        {
            await dispatcher.PublishEventAsync(new HelloSaidEvent());
            return Result.Ok();
        }
    }
}
