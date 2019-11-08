using CQELight.DAL.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.Abstractions.DAL.Interfaces
{
    /// <summary>
    /// Contrat interface for repositories that performs data updates.
    /// </summary>
    public interface IDataUpdateRepository : IUnitOfWork
    {
        /// <summary>
        /// Mark an entity for insertion.
        /// </summary>
        /// <param name="entity">Entity to insert.</param>
        void MarkForInsert<T>(T entity) where T : class;
        /// <summary>
        /// Mark a range of entities for insertion.
        /// </summary>
        /// <param name="entities">Entities to insert.</param>
        void MarkForInsertRange<T>(IEnumerable<T> entities) where T : class;
        /// <summary>
        /// Mark an entity for update.
        /// </summary>
        /// <param name="entity">Entity to update.</param>
        void MarkForUpdate<T>(T entity) where T : class;
        /// <summary>
        /// Mark a range of entities for update.
        /// </summary>
        /// <param name="entities">Entities to update.</param>
        void MarkForUpdateRange<T>(IEnumerable<T> entities) where T : class;
        /// <summary>
        /// Mark an entity for deletion.
        /// </summary>
        /// <param name="entityToDelete">Entity to delete.</param>
        /// <param name="physicalDeletion">Flag to use physical deletion.</param>
        void MarkForDelete<T>(T entityToDelete, bool physicalDeletion = false) where T : class;
        /// <summary>
        /// Mark a range of entities for deletion.
        /// </summary>
        /// <param name="entitiesToDelete">Entities to delete.</param>
        /// <param name="physicalDeletion">Flag to use physical deletion.</param>
        void MarkForDeleteRange<T>(IEnumerable<T> entitiesToDelete, bool physicalDeletion = false) where T : class;
        /// <summary>
        /// Mark an entity by its id for deletion.
        /// </summary>
        /// <param name="id">Id of the entity to delete.</param>
        /// <param name="physicalDeletion">Flag to use physical deletion.</param>
        void MarkIdForDelete<T>(object id, bool physicalDeletion = false) where T : class;
    }
}
