using CQELight.Abstractions.Events;
using Geneao.Identity;
using System;

namespace Geneao.Events
{
    public sealed class PersonneAjoutee : BaseDomainEvent
    {
        public NomFamille NomFamille { get; private set; }
        public string Prenom { get; private set; }
        public string LieuNaissance { get; private set; }
        public DateTime DateNaissance { get; private set; }

        private PersonneAjoutee() { }

        internal PersonneAjoutee(NomFamille nomFamille, string prenom, string lieuNaissance, DateTime dateNaissance)
        {
            if (string.IsNullOrWhiteSpace(prenom)) throw new ArgumentException("PersonneAjouteeEvent.Ctor() : Prénom requis.", nameof(prenom));

            if (string.IsNullOrWhiteSpace(lieuNaissance)) throw new ArgumentException("PersonneAjouteeEvent.Ctor() : Lieu naissance requis.", nameof(lieuNaissance));

            DateNaissance = dateNaissance;
            LieuNaissance = lieuNaissance;
            Prenom = prenom;
            NomFamille = nomFamille;
        }
    }

}
