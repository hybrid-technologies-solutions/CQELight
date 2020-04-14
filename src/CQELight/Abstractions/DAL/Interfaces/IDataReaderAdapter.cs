using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CQELight.Abstractions.DAL.Interfaces
{
    /// <summary>
    /// Contract interface for DAL DataReader Adapter.
    /// </summary>
    public interface IDataReaderAdapter : IDisposable
    {
        /// <summary>
        /// Get asynchronously a bunch of entites from repository, by applying filter, order and some other.
        /// </summary>
        /// <param name="filter">Specific filter to apply on entities.</param>
        /// <param name="orderBy">Order to apply when retrieving entities.</param>
        /// <param name="includeDeleted">Flag to indicates if soft deleted entites should be included.</param>
        /// <typeparam name="T">Type of entity to look for</typeparam>
        /// <returns>Bunch of entites that respects defined parameters.</returns>
        IAsyncEnumerable<T> GetAsync<T>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool includeDeleted = false)
            where T : class;

        /// <summary>
        /// Get asynchronously an entity by its id.
        /// </summary>
        /// <typeparam name="T">Type of entity to retrieve by Id</typeparam>
        /// <param name="value">Id value.</param>
        /// <returns>Entity that matches Id value.</returns>
        Task<T> GetByIdAsync<T>(object value)
            where T : class;
    }
}
