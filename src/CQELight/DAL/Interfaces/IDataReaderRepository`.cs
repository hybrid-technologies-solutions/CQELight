using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CQELight.DAL.Interfaces
{
    /// <summary>
    /// Base contract interface for reader repository.
    /// </summary>
    /// <typeparam name="T">Type of entity to read.</typeparam>
    [Obsolete("This repository per entity is not supported anymore. Migrate to BaseRepository with IDataReaderAdapter")]
    public interface IDataReaderRepository<T>
        where T : IPersistableEntity
    {

        /// <summary>
        /// Get asynchronously a bunch of entites from repository, by applying filter, order and some other.
        /// </summary>
        /// <param name="filter">Specific filter to apply on entities.</param>
        /// <param name="orderBy">Order to apply when retrieving entities.</param>
        /// <param name="includeDeleted">Flag to indicates if soft deleted entites should be included.</param>
        /// <param name="includes">Array of properties of linked elements that should be eager loaded.</param>
        /// <returns>Bunch of entites that respects defined parameters.</returns>
        IAsyncEnumerable<T> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool includeDeleted = false,
            params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Get asynchronously an entity by its id.
        /// </summary>
        /// <typeparam name="TId">Type of Id.</typeparam>
        /// <param name="value">Id value.</param>
        /// <returns>Entity that matches Id value.</returns>
        Task<T> GetByIdAsync<TId>(TId value) where TId : notnull;
    }
}