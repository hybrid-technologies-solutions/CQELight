using CQELight.DAL.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.DAL.Interfaces
{
    /// <summary>
    /// Contrat interface for repositories that performs data updates.
    /// </summary>
    [Obsolete("This repository per entity is not supported anymore. Use IDataUpdateRepository without type instead")]
    public interface IDataUpdateRepository<T>
        where T : IPersistableEntity
    {
        /// <summary>
        /// Asynchronously saves all modifications into repository.
        /// </summary>
        /// <returns>Number of modifications performed.</returns>
        Task<int> SaveAsync();
        /// <summary>
        /// Mark an entity for insertion.
        /// </summary>
        /// <param name="entity">Entity to insert.</param>
        void MarkForInsert(T entity);
        /// <summary>
        /// Mark a range of entities for insertion.
        /// </summary>
        /// <param name="entities">Entities to insert.</param>
        void MarkForInsertRange(IEnumerable<T> entities);
        /// <summary>
        /// Mark an entity for update.
        /// </summary>
        /// <param name="entity">Entity to update.</param>
        void MarkForUpdate(T entity);
        /// <summary>
        /// Mark a range of entities for update.
        /// </summary>
        /// <param name="entities">Entities to update.</param>
        void MarkForUpdateRange(IEnumerable<T> entities);
        /// <summary>
        /// Mark an entity for deletion.
        /// </summary>
        /// <param name="entityToDelete">Entity to delete.</param>
        /// <param name="physicalDeletion">Flag to use physical deletion.</param>
        void MarkForDelete(T entityToDelete, bool physicalDeletion = false);
        /// <summary>
        /// Mark a range of entities for deletion.
        /// </summary>
        /// <param name="entitiesToDelete">Entities to delete.</param>
        /// <param name="physicalDeletion">Flag to use physical deletion.</param>
        void MarkForDeleteRange(IEnumerable<T> entitiesToDelete, bool physicalDeletion = false);
        /// <summary>
        /// Mark an entity by its id for deletion.
        /// </summary>
        /// <param name="id">Id of the entity to delete.</param>
        /// <param name="physicalDeletion">Flag to use physical deletion.</param>
        void MarkIdForDelete<TId>(TId id, bool physicalDeletion = false);
    }
}
