using CQELight.DAL.Attributes;
using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CQELight.DAL.EFCore
{
    /// <summary>
    /// Entity Framework Core Repository implementation.
    /// </summary>
    /// <typeparam name="T">Type of entity to manage.</typeparam>
    [Obsolete("This implementation of EF Repository is no longer maintained. Use DataRepository with EFDataReaderAdapter & EFDataWriterAdapter.")]
    public class EFRepository<T> : DisposableObject, IDatabaseRepository<T>
        where T : class, IPersistableEntity
    {
        #region Members

        private bool _createMode;
        private readonly SemaphoreSlim _lock;

        #endregion

        #region Properties

        protected DbSet<T> DataSet => Context.Set<T>();
        protected BaseDbContext Context { get; }
        protected bool Disposed { get; set; }
        protected ICollection<IPersistableEntity> _added { get; set; }
        protected ICollection<IPersistableEntity> _modified { get; set; }
        protected ICollection<IPersistableEntity> _deleted { get; set; }
        protected List<string> _deleteSqlQueries = new List<string>();
        private IStateManager StateManager => Context.GetService<IStateManager>();

        #endregion

        #region Constructor

        public EFRepository(BaseDbContext context)
        {
            Context = context;
            _added = new List<IPersistableEntity>();
            _modified = new List<IPersistableEntity>();
            _deleted = new List<IPersistableEntity>();
            _lock = new SemaphoreSlim(1);
        }

        #endregion

        #region IDataReaderRepository methods

        public IEnumerable<T> Get(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool includeDeleted = false,
            params Expression<Func<T, object>>[] includes)
            => GetCore(filter, orderBy, includeDeleted, includes).AsEnumerable();

        public IAsyncEnumerable<T> GetAsync(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool includeDeleted = false,
            params Expression<Func<T, object>>[] includes)
            => GetCore(filter, orderBy, includeDeleted, includes)
#if NETSTANDARD2_0
            .ToAsyncEnumerable();
#elif NETSTANDARD2_1
            .AsAsyncEnumerable();
#endif

        public async Task<T> GetByIdAsync<TId>(TId value)
            => await DataSet.FindAsync(value).ConfigureAwait(false);

        #endregion

        #region IDataUpdateRepository methods

        public virtual async Task<int> SaveAsync()
        {
            int dbResults = 0;

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                _deleteSqlQueries.DoForEach(q => Context.Database.ExecuteSqlCommand(q));
                _deleteSqlQueries.Clear();
                dbResults = await Context.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
            _added.Clear();
            _modified.Clear();
            _deleted.Clear();
            _createMode = false;
            return dbResults;
        }

        public virtual void MarkForUpdate(T entity)
            => MarkEntityForUpdate(entity);

        public virtual void MarkForInsert(T entity)
            => MarkEntityForInsert(entity);

        public virtual void MarkForDelete(T entityToDelete, bool physicalDeletion = false)
        {
            if (physicalDeletion || EFCoreInternalExecutionContext.DisableLogicalDeletion)
            {
                StateManager.GetOrCreateEntry(entityToDelete).SetEntityState(EntityState.Deleted, true);
            }
            else
            {
                MarkEntityForSoftDeletion(entityToDelete);
            }
            _deleted.Add(entityToDelete);
        }

        public void MarkForInsertRange(IEnumerable<T> entities)
            => entities.DoForEach(MarkForInsert);

        public void MarkForUpdateRange(IEnumerable<T> entities)
            => entities.DoForEach(MarkForUpdate);

        public void MarkForDeleteRange(IEnumerable<T> entitiesToDelete, bool physicalDeletion = false)
            => entitiesToDelete.DoForEach(e => MarkForDelete(e, physicalDeletion));

        public void MarkIdForDelete<TId>(TId id, bool physicalDeletion = false)
        {
            if (id?.Equals(default(TId)) == true)
            {
                throw new ArgumentNullException(nameof(id));
            }
            var instance = Context.Find<T>(id);
            if (instance == null)
            {
                throw new InvalidOperationException($"EFRepository.MarkIdForDelete() :" +
                    $" Cannot delete of type '{typeof(T).FullName}' with '{id}' because it doesn't exists anymore into database.");
            }
            MarkForDelete(instance, physicalDeletion);
        }

        #endregion

        #region ISQLRepository

        public Task<int> ExecuteSQLCommandAsync(string sql)
             => Context.Database.ExecuteSqlCommandAsync(sql);

        public async Task<TResult> ExecuteScalarAsync<TResult>(string sql)
        {
            var connection = Context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return (TResult)await command.ExecuteScalarAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region protected virtual methods

        protected virtual void MarkEntityForUpdate<TEntity>(TEntity entity)
            where TEntity : class, IPersistableEntity
        {
            _lock.Wait();
            if (entity is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.EditDate = DateTime.Now;
            }
            _modified.Add(entity);
            _createMode = false;
            Context.ChangeTracker.TrackGraph(entity, TrackGraph);
            _lock.Release();
        }

        protected virtual void MarkEntityForInsert<TEntity>(TEntity entity)
            where TEntity : class, IPersistableEntity
        {
            _lock.Wait();
            if (entity is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.EditDate = DateTime.Now;
            }
            _added.Add(entity);
            _createMode = true;
            Context.ChangeTracker.TrackGraph(entity, TrackGraph);
            _lock.Release();
        }

        protected virtual void MarkEntityForSoftDeletion<TEntity>(TEntity entityToDelete)
            where TEntity : class, IPersistableEntity
        {
            if (entityToDelete is BasePersistableEntity basePersistableEntity)
            {
                basePersistableEntity.Deleted = true;
                basePersistableEntity.DeletionDate = DateTime.Now;
            }

            StateManager.GetOrCreateEntry(entityToDelete).SetEntityState(EntityState.Modified, true);
        }

        protected virtual IQueryable<T> GetCore(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool includeDeleted = false,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = DataSet;
            if (typeof(T).IsSubclassOf(typeof(BasePersistableEntity)) && !includeDeleted)
            {
                query = includeDeleted ? DataSet : DataSet.Where(m => !EF.Property<bool>(m, nameof(BasePersistableEntity.Deleted)));
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }
            if (includes?.Any() == true)
            {
                foreach (var incl in includes)
                {
                    query = query.Include(incl);
                }
            }
            if (orderBy != null)
            {
                return query.OrderBy(orderBy).AsQueryable();
            }
            else
            {
                return query;
            }
        }

        protected void TrackGraph(EntityEntryGraphNode obj)
        {
            var navAttr = obj.InboundNavigation?.PropertyInfo?.GetCustomAttribute<NotNaviguableAttribute>();
            if (CannotNaviguate(navAttr))
            {
                obj.Entry.State = EntityState.Unchanged;
                return;
            }
            if (obj.Entry.Entity is BasePersistableEntity baseEntity)
            {
                baseEntity.EditDate = DateTime.Now;
            }
            if (obj.Entry.IsKeySet)
            {
                if (_createMode || obj.Entry.GetDatabaseValues() == null)
                {
                    obj.Entry.State = EntityState.Added;
                }
                else
                {
                    obj.Entry.State = EntityState.Modified;
                }
            }
            else
            {
                obj.Entry.State = EntityState.Added;
            }
        }

        #endregion

        #region Private methods

        private bool CannotNaviguate(NotNaviguableAttribute navAttr)
        {
            return navAttr != null &&
                            ((_createMode && navAttr.Mode.HasFlag(NavigationMode.Create)) || (!_createMode && navAttr.Mode.HasFlag(NavigationMode.Update)));
        }

        #endregion

        #region IDisposable methods

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Context.Dispose();
                }
            }
            this.Disposed = true;
        }
        #endregion

    }
}
