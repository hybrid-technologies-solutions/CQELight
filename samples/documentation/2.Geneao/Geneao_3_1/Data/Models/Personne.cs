using CQELight.DAL.Attributes;
using CQELight.DAL.Interfaces;
using System;

namespace Geneao.Data.Models
{
    [Table("Personnes")]
    public class Personne : IPersistableEntity
    {
        [PrimaryKey]
        public Guid PersonneId { get; set; }
        [Column]
        public string Prenom { get; set; }
        [Column]
        public string LieuNaissance { get; set; }
        [Column]
        public DateTime DateNaissance { get; set; }
        [ForeignKey]
        public Famille Famille { get; set; }
        [Column("NomFamille"), KeyStorageOf(nameof(Famille))]
        public string Famille_Id { get; set; }

        public object GetKeyValue()
            => PersonneId;

        public bool IsKeySet()
            => PersonneId != Guid.Empty;
    }
}
