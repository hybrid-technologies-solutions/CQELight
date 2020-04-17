using System;

namespace Geneao.Identity
{
    public struct PersonneId
    {
        public Guid Value { get; private set; }

        public PersonneId(Guid value)
        {
            if (value == Guid.Empty)
                throw new InvalidOperationException("PersonneId.ctor() : Un identifiant valide doit être fourni.");
            Value = value;
        }

        public static PersonneId Generate()
            => new PersonneId(Guid.NewGuid());
    }
}
