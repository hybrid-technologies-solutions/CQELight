using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using Geneao.Data;
using Geneao.Events;
using Geneao.Queries.Models.Out;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Geneao.Queries
{
    class FamilleCreeeInvalider : IDomainEventHandler<FamilleCreee>
    {
        public Task<Result> HandleAsync(FamilleCreee domainEvent, IEventContext context = null)
        {
            RecupererListeFamille.AjouterFamilleAuCache(domainEvent.NomFamille.Value);
            return Result.Ok();
        }
    }
    public interface IRecupererListeFamille : IQuery<IEnumerable<FamilleListItem>> { }
    class RecupererListeFamille : IRecupererListeFamille, IAutoRegisterType
    {
        internal static void AjouterFamilleAuCache(string nom) => s_Cache.Add(nom);

        private static ConcurrentBag<string> s_Cache
            = new ConcurrentBag<string>();

        private readonly IFamilleRepository _familleRepository;

        public RecupererListeFamille(IFamilleRepository familleRepository)
        {
            _familleRepository = familleRepository ?? throw new ArgumentNullException(nameof(familleRepository));
        }

        public async Task<IEnumerable<FamilleListItem>> ExecuteQueryAsync()
        {
            if (s_Cache.IsEmpty)
            {
                s_Cache = new ConcurrentBag<string>((await _familleRepository.GetAllFamillesAsync().ConfigureAwait(false)).Select(f => f.Nom));
            }
            return s_Cache.Select(v => new FamilleListItem { Nom = v });
        }
    }
}
