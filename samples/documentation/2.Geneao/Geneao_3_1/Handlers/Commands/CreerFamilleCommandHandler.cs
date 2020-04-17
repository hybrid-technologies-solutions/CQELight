using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Dispatcher;
using Geneao.Commands;
using Geneao.Data;
using Geneao.Domain;
using Geneao.Events;
using Geneao.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace Geneao.Handlers.Commands
{
    class CreerFamilleCommandHandler : ICommandHandler<CreerFamilleCommand>, IAutoRegisterType
    {
        private readonly IFamilleRepository _familleRepository;

        public CreerFamilleCommandHandler(IFamilleRepository familleRepository)
        {
            _familleRepository = familleRepository ?? throw new System.ArgumentNullException(nameof(familleRepository));
        }

        public async Task<Result> HandleAsync(CreerFamilleCommand command, ICommandContext context = null)
        {
            Famille._nomFamilles = (await _familleRepository.GetAllFamillesAsync().ConfigureAwait(false)).Select(f => new NomFamille(f.Nom)).ToList();
            var result = Famille.CreerFamille(command.Nom);
            if(result && result is Result<NomFamille> resultFamille)
            {
                await CoreDispatcher.PublishEventAsync(new FamilleCreee(resultFamille.Value));
                return Result.Ok();
            }
            return result; 
        }
    }
}
