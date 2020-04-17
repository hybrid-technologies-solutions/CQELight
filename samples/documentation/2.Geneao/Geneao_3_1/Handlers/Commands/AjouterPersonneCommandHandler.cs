using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using Geneao.Commands;
using Geneao.Domain;
using System.Threading.Tasks;

namespace Geneao.Handlers.Commands
{
    class AjouterPersonneCommandHandler : ICommandHandler<AjouterPersonneCommand>, IAutoRegisterType
    {
        private readonly IAggregateEventStore _eventStore;

        public AjouterPersonneCommandHandler(IAggregateEventStore eventStore)
        {
            _eventStore = eventStore;
        }
        public async Task<Result> HandleAsync(AjouterPersonneCommand command, ICommandContext context = null)
        {
            var famille = await _eventStore.GetRehydratedAggregateAsync<Famille>(command.NomFamille);
            famille.AjouterPersonne(command.Prenom, new InfosNaissance(command.LieuNaissance, command.DateNaissance));
            await famille.PublishDomainEventsAsync();
            return Result.Ok();
        }
    }

}
