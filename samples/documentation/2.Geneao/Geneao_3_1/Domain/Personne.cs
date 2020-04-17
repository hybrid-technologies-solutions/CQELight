using CQELight.Abstractions.DDD;
using Geneao.Identity;
using System;

namespace Geneao.Domain
{
    public enum DeclarationNaissanceImpossibleCar
    {
        AbsenceDePrenom,
        AbsenceInformationNaissance,
    }

    public class Personne : Entity<PersonneId>
    {

        #region Properties

        public string Prenom { get; internal set; }
        public InfosNaissance InfosNaissance { get; internal set; }
        public DateTime? DateDeces { get; internal set; }

        #endregion

        #region Ctor

        private Personne(PersonneId id)
        {
            Id = id;
        }

        internal Personne() { }

        #endregion

        #region Public static methods

        public static Result DeclarerNaissance(string prenom, InfosNaissance infosNaissance)
        {
            if (string.IsNullOrWhiteSpace(prenom))
            {
                return Result.Fail(DeclarationNaissanceImpossibleCar.AbsenceDePrenom);
            }

            if (infosNaissance == null)
            {
                return Result.Fail(DeclarationNaissanceImpossibleCar.AbsenceInformationNaissance);
            }

            return Result.Ok(new Personne(PersonneId.Generate())
            {
                Prenom = prenom,
                InfosNaissance = infosNaissance
            });
        }

        #endregion

        #region Public methods

        public Result DeclarerDeces(DateTime dateDeces)
        {
            DateDeces = dateDeces;
            return Result.Ok();
        }

        #endregion

    }

}
