using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using Geneao.Data;
using Geneao.Events;
using System;
using System.Threading.Tasks;

namespace Geneao.Handlers.Events
{
    class PersonneAjouteeEventHandler : IDomainEventHandler<PersonneAjoutee>, IAutoRegisterType
    {
        private readonly IFamilleRepository _familleRepository;

        public PersonneAjouteeEventHandler(IFamilleRepository familleRepository)
        {
            _familleRepository = familleRepository ?? throw new ArgumentNullException(nameof(familleRepository));
        }
        public async Task<Result> HandleAsync(PersonneAjoutee domainEvent, IEventContext context = null)
        {
            var famille = await _familleRepository.GetFamilleByNomAsync(domainEvent.NomFamille);
            if (famille != null)
            {
                famille.Personnes.Add(new Data.Models.Personne
                {
                    DateNaissance = domainEvent.DateNaissance,
                    LieuNaissance = domainEvent.LieuNaissance,
                    Prenom = domainEvent.Prenom
                });
                await _familleRepository.SauverFamilleAsync(famille);
            }
                var color = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.DarkGreen;

                Console.WriteLine($"{domainEvent.Prenom} a correctement été ajouté(e) à la famille {domainEvent.NomFamille.Value}.");

                Console.ForegroundColor = color;


                return Result.Ok();
        }
    }

}
