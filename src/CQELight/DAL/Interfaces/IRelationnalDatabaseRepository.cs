using CQELight.DAL.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.DAL.Interfaces
{
    /// <summary>
    /// Contract interface for repository for relationnal databases.
    /// </summary>
    /// <typeparam name="T">Type of entity to manage into database.</typeparam>
    [Obsolete("This IRelationnalDatabaseRepository is not supported anymore")]
    public interface IRelationnalDatabaseRepository<T> : IDatabaseRepository<T>, ISqlRepository
        where T : IPersistableEntity
    {

    }
}
