using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using Geneao.Data;
using Geneao.Events;
using System;
using System.Threading.Tasks;

namespace Geneao.Handlers.Events
{
    class FamilleCreeeEventHandler : IDomainEventHandler<FamilleCreee>, IAutoRegisterType
    {
        private readonly IFamilleRepository _familleRepository;

        public FamilleCreeeEventHandler(IFamilleRepository familleRepository)
        {
            _familleRepository = familleRepository ?? throw new ArgumentNullException(nameof(familleRepository));
        }

        public async Task<Result> HandleAsync(FamilleCreee domainEvent, IEventContext context = null)
        {
            var color = Console.ForegroundColor;
            try
            {
                await _familleRepository.SauverFamilleAsync(new Data.Models.Famille
                {
                    Nom = domainEvent.NomFamille.Value
                }).ConfigureAwait(false);

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"La famille {domainEvent.NomFamille.Value} a correctement" +
                    $" été créée dans le système.");

            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"La famille {domainEvent.NomFamille.Value} n'a pas pu être" +
                    $" créée dans le système.");
                Console.WriteLine(e.ToString());
                return Result.Fail();
            }
            finally
            {
                Console.ForegroundColor = color;
            }
            return Result.Ok();
        }
    }
}
