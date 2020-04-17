using CQELight.Abstractions.CQS.Interfaces;
using System;

namespace Geneao.Commands
{
    public sealed class CreerFamilleCommand : ICommand
    {
        public string Nom { get; private set; }

        private CreerFamilleCommand() { }

        public CreerFamilleCommand(string nom)
        {
            if (string.IsNullOrWhiteSpace(nom))
            {
                throw new ArgumentException("CreerFamilleCommand.ctor() : Un nom doit être fourni.", nameof(nom));
            }

            Nom = nom;
        }
    }
}
