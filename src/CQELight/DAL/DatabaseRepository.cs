using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.DAL
{
    /// <summary>
    /// A standard base database repository.
    /// </summary>
    public class DatabaseRepository : DisposableObject, IDatabaseRepository
    {
        #region Members

        protected readonly IDataReaderAdapter dataReaderAdapter;
        protected readonly IDataWriterAdapter dataWriterAdapter;
        protected readonly List<object> added;
        protected readonly List<object> updated;
        protected readonly List<object> deleted;

        private bool saveInProgress;

        #endregion

        #region Ctor

        public DatabaseRepository(
            IDataReaderAdapter dataReaderAdapter,
            IDataWriterAdapter dataWriterAdapter)
        {
            this.dataReaderAdapter = dataReaderAdapter;
            this.dataWriterAdapter = dataWriterAdapter;
            added = new List<object>();
            updated = new List<object>();
            deleted = new List<object>();
        }

        #endregion

        #region Public methods

        public virtual IAsyncEnumerable<T> GetAsync<T>(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool includeDeleted = false) where T : class
            => dataReaderAdapter.GetAsync<T>(filter, orderBy, includeDeleted);

        public virtual Task<T> GetByIdAsync<T>(object value) where T : class
            => dataReaderAdapter.GetByIdAsync<T>(value);

        public virtual void MarkForDelete<T>(T entityToDelete, bool physicalDeletion = false) where T : class
        {
            if (physicalDeletion)
            {
                deleted.Add(entityToDelete);
            }
            else
            {
                if (entityToDelete is BasePersistableEntity basePersistableEntity)
                {
                    basePersistableEntity.Deleted = true;
                    basePersistableEntity.DeletionDate = DateTime.UtcNow;
                    updated.Add(basePersistableEntity);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unable to perform soft deletion of object of type {typeof(T).FullName}. " +
                        "You should override MarkForDelete to implement it by yourself.");
                }
            }
        }

        public void MarkForDeleteRange<T>(IEnumerable<T> entitiesToDelete, bool physicalDeletion = false) where T : class
            => entitiesToDelete.DoForEach(e => MarkForDelete(e, physicalDeletion));

        public void MarkForInsert<T>(T entity) where T : class
            => MarkEntityForInsert(entity);

        public void MarkForInsertRange<T>(IEnumerable<T> entities) where T : class
            => entities.DoForEach(MarkForInsert);

        public void MarkForUpdate<T>(T entity) where T : class
            => MarkEntityForUpdate(entity);

        public void MarkForUpdateRange<T>(IEnumerable<T> entities) where T : class
            => entities.DoForEach(MarkForUpdate);

        public void MarkIdForDelete<T>(object id, bool physicalDeletion = false) where T : class
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            var instance = dataReaderAdapter.GetByIdAsync<T>(id).GetAwaiter().GetResult();
            if (instance == null)
            {
                throw new InvalidOperationException($" Cannot delete of type '{typeof(T).FullName}' with '{id}' because it doesn't exists anymore into database.");
            }
            MarkForDelete(instance, physicalDeletion);
        }

        public async Task<int> SaveAsync()
        {
            if (saveInProgress)
            {
                throw new InvalidOperationException("A current save operation is currently in progress. You'll have to wait before doing a new one on the same repository or add more operation in the same unit of work scope.");
            }
            saveInProgress = true;
            try
            {
                await added.DoForEachAsync(dataWriterAdapter.InsertAsync);
                await updated.DoForEachAsync(dataWriterAdapter.UpdateAsync);
                await deleted.DoForEachAsync(dataWriterAdapter.DeleteAsync);
                int dbResults = await dataWriterAdapter.SaveAsync().ConfigureAwait(false);
                added.Clear();
                updated.Clear();
                deleted.Clear();
                return dbResults;
            }
            finally
            {
                saveInProgress = false;
            }
        }

        #endregion

        #region Private methods

        protected virtual void MarkEntityForUpdate<TEntity>(TEntity entity)
           where TEntity : class
        {
            if (entity is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.EditDate = DateTime.Now;
            }
            updated.Add(entity);
        }

        protected virtual void MarkEntityForInsert<TEntity>(TEntity entity)
            where TEntity : class
        {
            if (entity is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.EditDate = DateTime.Now;
            }
            added.Add(entity);
        }

        protected virtual void MarkEntityForSoftDeletion<TEntity>(TEntity entityToDelete)
            where TEntity : class
        {
            if (entityToDelete is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.Deleted = true;
                basePersistableEntity.DeletionDate = DateTime.Now;
            }
            updated.Add(entityToDelete);
        }

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            try
            {
                dataReaderAdapter.Dispose();
                dataWriterAdapter.Dispose();
            }
            catch
            {
                //No throw on disposing
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
