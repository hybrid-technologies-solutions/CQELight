using CQELight.DAL.Attributes;
using CQELight.DAL.Interfaces;
using System.Collections.Generic;

namespace Geneao.Data.Models
{
    [Table("Familles")]
    public class Famille : IPersistableEntity
    {
        [PrimaryKey]
        public string Nom { get; set; }
        public ICollection<Personne> Personnes { get; set; }
            = new List<Personne>();

        public object GetKeyValue()
            => Nom;

        public bool IsKeySet()
            => !string.IsNullOrWhiteSpace(Nom);
    }
}
