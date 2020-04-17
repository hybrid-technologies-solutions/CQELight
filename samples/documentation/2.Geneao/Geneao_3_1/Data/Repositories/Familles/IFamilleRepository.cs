using Geneao.Data.Models;
using Geneao.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Geneao.Data
{
    interface IFamilleRepository
    {
        Task SauverFamilleAsync(Famille famille);
        Task<Famille> GetFamilleByNomAsync(NomFamille nomFamille);
        Task<IEnumerable<Famille>> GetAllFamillesAsync();
    }
}
