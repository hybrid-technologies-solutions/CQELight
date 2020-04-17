using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using Geneao.Events;
using Geneao.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geneao.Domain
{

    public enum PersonneNonAjouteeCar
    {
        PrenomInvalide,
        InformationsDeNaissanceInvalides,
        PersonneExistante
    }
    public enum FamilleNonCreeeCar
    {
        NomIncorrect,
        FamilleDejaExistante
    }

    public class Famille : AggregateRoot<NomFamille>, IEventSourcedAggregate
    {
        internal static List<NomFamille> _nomFamilles = new List<NomFamille>();

        public IEnumerable<Personne> Personnes => _state.Personnes.AsEnumerable();

        private FamilleState _state;

        private class FamilleState : AggregateState
        {
            public List<Personne> Personnes { get; set; }
            public NomFamille Nom { get; set; }

            public FamilleState()
            {
                Personnes = new List<Personne>();
                AddHandler<FamilleCreee>(FamilleCree);
                AddHandler<PersonneAjoutee>(OnPersonneAjoutee);
            }

            private void OnPersonneAjoutee(PersonneAjoutee obj)
            {
                Personnes.Add(new Personne
                {
                    DateDeces = null,
                    Prenom = obj.Prenom,
                    InfosNaissance = new InfosNaissance(obj.LieuNaissance, obj.DateNaissance)
                });
            }

            private void FamilleCree(FamilleCreee obj)
            {
                Nom = obj.NomFamille;
                _nomFamilles.Add(obj.NomFamille);
            }
        }

        private Famille() { }

        public Famille(NomFamille nomFamille, IEnumerable<Personne> personnes = null)
        {
            if (!_nomFamilles.Any(f => f.Value.Equals(nomFamille.Value, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Famille.Ctor() : Impossible de créer une famille qui n'a pas été d'abord créée dans le système.");
            }
            Id = nomFamille;
            _state = new FamilleState
            {
                Personnes = (personnes ?? Enumerable.Empty<Personne>()).ToList()
            };
        }

        public void RehydrateState(IEnumerable<IDomainEvent> events)
        {
            _state.ApplyRange(events);
            Id = _state.Nom;
        }

        public static Result CreerFamille(string nom, IEnumerable<Personne> personnes = null)
        {
            NomFamille nomFamille = new NomFamille();
            try
            {
                nomFamille = new NomFamille(nom);
            }
            catch
            {
                return Result.Fail(FamilleNonCreeeCar.NomIncorrect);
            }
            if (_nomFamilles.Any(f => f.Value.Equals(nom, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Fail(FamilleNonCreeeCar.FamilleDejaExistante);
            }
            _nomFamilles.Add(nomFamille);
            return Result.Ok(nomFamille);
        }

        public Result AjouterPersonne(string prenom, InfosNaissance infosNaissance)
        {
            PersonneNonAjouteeCar? raison = null;
            if (string.IsNullOrWhiteSpace(prenom))
            {
                raison = PersonneNonAjouteeCar.PrenomInvalide;
            }
            else
            {
                if (!_state.Personnes.Any(p => p.Prenom == prenom && p.InfosNaissance == infosNaissance))
                {
                    var declarationNaissance = Personne.DeclarerNaissance(prenom, infosNaissance);
                    if (declarationNaissance && declarationNaissance is Result<Personne> declarationOk)
                    {
                        _state.Personnes.Add(declarationOk.Value);
                        AddDomainEvent(new PersonneAjoutee(Id, prenom, infosNaissance.Lieu, infosNaissance.DateNaissance));
                        return Result.Ok();
                    }
                    else
                    {
                        return declarationNaissance;
                    }
                }
                else
                {
                    raison = PersonneNonAjouteeCar.PersonneExistante;
                }
            }
            return Result.Fail(raison.Value);
        }
    }

}
