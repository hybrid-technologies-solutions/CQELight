using System;

namespace Geneao.Identity
{
    public struct NomFamille
    {
        public string Value { get; private set; }

        public NomFamille(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
                throw new InvalidOperationException("NomFamille.ctor() : Un nom de famille correct doit être fourni (non vide et inférieur à 128 caractères).");
            Value = value;
        }

        public static implicit operator NomFamille(string nom)
            => new NomFamille(nom);
    }
}
