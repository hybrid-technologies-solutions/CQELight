using CQELight.Abstractions.DDD;
using System;

namespace Geneao.Domain
{
    public class InfosNaissance : ValueObject<InfosNaissance>
    {

        #region Properties

        public string Lieu { get; private set; }
        public DateTime DateNaissance { get; private set; }

        #endregion

        #region Ctor

        public InfosNaissance(string lieu, DateTime dateNaissance)
        {
            if (string.IsNullOrWhiteSpace(lieu))
            {
                throw new ArgumentException("InfosNaissance.Ctor() : Lieu requis.", nameof(lieu));
            }

            Lieu = lieu;
            DateNaissance = dateNaissance;
        }

        #endregion

        #region Overriden methods

        protected override bool EqualsCore(InfosNaissance other)
        => other.DateNaissance == DateNaissance && other.Lieu == Lieu;

        protected override int GetHashCodeCore()
        => (typeof(InfosNaissance).AssemblyQualifiedName + DateNaissance + Lieu).GetHashCode();

        #endregion

    }

}
