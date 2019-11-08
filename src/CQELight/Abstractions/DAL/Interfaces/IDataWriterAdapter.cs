using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.Abstractions.DAL.Interfaces
{
    /// <summary>
    /// Contract interface for DAL DataWriter adapter.
    /// </summary>
    public interface IDataWriterAdapter : IUnitOfWork, IDisposable
    {
        /// <summary>
        /// Insert a new entity.
        /// </summary>
        /// <typeparam name="T">Typeof of entity to insert</typeparam>
        /// <param name="entity">Entity to insert</param>
        Task InsertAsync<T>(T entity) where T : class;
        /// <summary>
        /// Update an entity
        /// </summary>
        /// <typeparam name="T">Typeof entity to update</typeparam>
        /// <param name="entity">Entity to update</param>
        Task UpdateAsync<T>(T entity) where T : class;
        /// <summary>
        /// Delete an entity
        /// </summary>
        /// <typeparam name="T">Typeof entity to delete</typeparam>
        /// <param name="entity">Entity to delete</param>
        Task DeleteAsync<T>(T entity) where T : class;


    }
}
