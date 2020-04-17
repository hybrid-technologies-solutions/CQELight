using CQELight.Abstractions.CQS.Interfaces;
using Geneao.Identity;
using System;

namespace Geneao.Commands
{
    public sealed class AjouterPersonneCommand : ICommand
    {
        public NomFamille NomFamille { get; private set; }
        public string Prenom { get; private set; }
        public string LieuNaissance { get; private set; }
        public DateTime DateNaissance { get; private set; }

        private AjouterPersonneCommand() { }

        public AjouterPersonneCommand(NomFamille nomFamille, string prenom, string lieuNaissance, DateTime dateNaissance)
        {
            if (string.IsNullOrWhiteSpace(prenom)) throw new ArgumentException("AjouterPersonneCommand.Ctor() : Prénom requis.", nameof(prenom));

            if (string.IsNullOrWhiteSpace(lieuNaissance)) throw new ArgumentException("AjouterPersonneCommand.Ctor() : Lieu naissance requis.", nameof(lieuNaissance));

            DateNaissance = dateNaissance;
            LieuNaissance = lieuNaissance;
            Prenom = prenom;
            NomFamille = nomFamille;
        }
    }

}
