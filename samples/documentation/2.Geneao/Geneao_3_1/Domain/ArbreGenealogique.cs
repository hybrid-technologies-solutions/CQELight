using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using Geneao.Events;
using Geneao.Identity;
using System;
using System.Collections.Generic;

namespace Geneao.Domain
{
    public class ArbreGenealogique : AggregateRoot<Guid>, IEventSourcedAggregate
    {

        private ArbreGenealogiqueState _state = new ArbreGenealogiqueState();

        private class ArbreGenealogiqueState : AggregateState
        {
            private List<NomFamille> Familles = new List<NomFamille>();

            public ArbreGenealogiqueState()
            {
                AddHandler<FamilleCreee>(OnFamilleCreee);
            }

            private void OnFamilleCreee(FamilleCreee obj)
            {
                Familles.Add(obj.NomFamille);
            }
        }

        public void RehydrateState(IEnumerable<IDomainEvent> events) => _state.ApplyRange(events);
    }
}
