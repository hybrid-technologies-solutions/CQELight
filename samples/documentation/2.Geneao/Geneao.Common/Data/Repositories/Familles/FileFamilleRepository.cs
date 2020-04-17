using CQELight.Abstractions.IoC.Interfaces;
using Geneao.Data.Models;
using Geneao.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Geneao.Data
{
    class FileFamilleRepository : IFamilleRepository, IAutoRegisterTypeSingleInstance
    {
        private ConcurrentBag<Famille> _familles = new ConcurrentBag<Famille>();
        private string _filePath;

        public FileFamilleRepository()
            : this(new FileInfo("./familles.json"))
        {

        }

        public FileFamilleRepository(FileInfo jsonFile)
        {
            _filePath = jsonFile.FullName;
            var familles = JsonConvert.DeserializeObject<IEnumerable<Famille>>(File.ReadAllText(_filePath));
            if (familles?.Any() == true)
            {
                _familles = new ConcurrentBag<Famille>(familles);
            }
        }

        public Task<IEnumerable<Famille>> GetAllFamillesAsync()
            => Task.FromResult(_familles.AsEnumerable());

        public Task<Famille> GetFamilleByNomAsync(NomFamille nomFamille)
            => Task.FromResult(_familles.FirstOrDefault(f => f.Nom.Equals(nomFamille.Value, StringComparison.OrdinalIgnoreCase)));

        public Task SauverFamilleAsync(Famille famille)
        {
            if (_familles.Any(f => f.Nom == famille.Nom))
            {
                _familles = new ConcurrentBag<Famille>(_familles.ToList().Where(f => f.Nom
                 != famille.Nom));
            }
                _familles.Add(famille);
            
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(_familles));
            return Task.CompletedTask;
        }
    }
}
