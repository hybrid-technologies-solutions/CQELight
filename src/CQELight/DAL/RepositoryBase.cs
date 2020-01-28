using CQELight.Abstractions.DAL.Interfaces;
using CQELight.DAL.Common;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.DAL
{
    /// <summary>
    /// A standard base database repository.
    /// </summary>
    public class RepositoryBase : DisposableObject, IDatabaseRepository
    {
        #region Members

        protected readonly IDataReaderAdapter dataReaderAdapter;
        protected readonly IDataWriterAdapter dataWriterAdapter;
        protected List<Task> markingTasks = new List<Task>();
        private SemaphoreSlim threadSafety = new SemaphoreSlim(1);

        #endregion

        #region Ctor

        public RepositoryBase(
            IDataReaderAdapter dataReaderAdapter,
            IDataWriterAdapter dataWriterAdapter)
        {
            this.dataReaderAdapter = dataReaderAdapter;
            this.dataWriterAdapter = dataWriterAdapter;
        }

        #endregion

        #region Public methods

        public virtual IAsyncEnumerable<T> GetAsync<T>(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool includeDeleted = false) where T : class
            => dataReaderAdapter.GetAsync(filter, orderBy, includeDeleted);

        public virtual Task<T> GetByIdAsync<T>(object value) where T : class
            => dataReaderAdapter.GetByIdAsync<T>(value);

        public virtual void MarkForDelete<T>(T entityToDelete, bool physicalDeletion = false) where T : class
        {
            threadSafety.Wait();
            try
            {
                markingTasks.Add(dataWriterAdapter.DeleteAsync(entityToDelete, physicalDeletion));
            }
            finally
            {
                threadSafety.Release();
            }
        }

        public virtual void MarkForDeleteRange<T>(IEnumerable<T> entitiesToDelete, bool physicalDeletion = false) where T : class
            => entitiesToDelete.DoForEach(e => MarkForDelete(e, physicalDeletion));

        public virtual void MarkForInsert<T>(T entity) where T : class
            => MarkEntityForInsert(entity);

        public virtual void MarkForInsertRange<T>(IEnumerable<T> entities) where T : class
            => entities.DoForEach(MarkForInsert);

        public virtual void MarkForUpdate<T>(T entity) where T : class
            => MarkEntityForUpdate(entity);

        public virtual void MarkForUpdateRange<T>(IEnumerable<T> entities) where T : class
            => entities.DoForEach(MarkForUpdate);

        public virtual void MarkIdForDelete<T>(object id, bool physicalDeletion = false) where T : class
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

        public virtual async Task<int> SaveAsync()
        {
            await threadSafety.WaitAsync();
            try
            {
                await Task.WhenAll(markingTasks);
                markingTasks.Clear();
                return await dataWriterAdapter.SaveAsync().ConfigureAwait(false);
            }
            finally
            {
                threadSafety.Release();
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
            threadSafety.Wait();
            try
            {
                markingTasks.Add(dataWriterAdapter.UpdateAsync(entity));
            }
            finally
            {
                threadSafety.Release();
            }
        }

        protected virtual void MarkEntityForInsert<TEntity>(TEntity entity)
            where TEntity : class
        {
            if (entity is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.EditDate = DateTime.Now;
            }
            threadSafety.Wait();
            try
            {
                markingTasks.Add(dataWriterAdapter.InsertAsync(entity));
            }
            finally
            {
                threadSafety.Release();
            }
        }

        protected virtual void MarkEntityForSoftDeletion<TEntity>(TEntity entityToDelete)
            where TEntity : class
        {
            if (entityToDelete is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.Deleted = true;
                basePersistableEntity.DeletionDate = DateTime.Now;
            }
            threadSafety.Wait();
            try
            {
                markingTasks.Add(dataWriterAdapter.UpdateAsync(entityToDelete));
            }
            finally
            {
                threadSafety.Release();
            }
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
