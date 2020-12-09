using CQELight.Abstractions.DAL.Interfaces;
using CQELight.DAL.Common;
using CQELight.Tools;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CQELight.DAL.EFCore.Adapters
{
    /// <summary>
    /// Data-reading adapter to use with EF Core.
    /// </summary>
    public class EFCoreDataReaderAdapter : DisposableObject, IDataReaderAdapter
    {
        #region Members

        private readonly BaseDbContext dbContext;
        private readonly EFCoreOptions? options;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new <see cref="EFCoreDataReaderAdapter"/> instance.
        /// </summary>
        /// <param name="dbContext">DbContext to use.</param>
        /// <param name="options">Custom working options.</param>
        public EFCoreDataReaderAdapter(
            BaseDbContext dbContext,
            EFCoreOptions? options = null)
        {
            this.dbContext = dbContext;
            this.options = options;
        }

        #endregion

        #region IDataReaderAdapter methods

        /// <summary>
        /// Get asynchronously a bunch of entites from repository, by applying filter, order and some other.
        /// </summary>
        /// <param name="filter">Specific filter to apply on entities.</param>
        /// <param name="orderBy">Order to apply when retrieving entities.</param>
        /// <param name="includeDeleted">Flag to indicates if soft deleted entites should be included.</param>
        /// <typeparam name="T">Type of entity to look for</typeparam>
        /// <returns>Bunch of entites that respects defined parameters.</returns>
        public IAsyncEnumerable<T> GetAsync<T>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool includeDeleted = false) where T : class
            => GetCore(filter, orderBy, includeDeleted)
#if NETSTANDARD2_0
            .ToAsyncEnumerable();
#elif NETSTANDARD2_1 || NET5_0
            .AsAsyncEnumerable();
#endif

        /// <summary>
        /// Get asynchronously an entity by its id.
        /// </summary>
        /// <typeparam name="T">Type of entity to retrieve by Id</typeparam>
        /// <param name="value">Id value.</param>
        /// <returns>Entity that matches Id value.</returns>
        public async Task<T> GetByIdAsync<T>(object value) where T : class
            => await dbContext.Set<T>().FindAsync(value).ConfigureAwait(false);

        #endregion

        #region Protected methods

        protected virtual IQueryable<T> GetCore<T>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool includeDeleted = false)
            where T : class
        {
            var dataSet = dbContext.Set<T>();
            IQueryable<T> query = dataSet;
            if (typeof(T).IsSubclassOf(typeof(BasePersistableEntity)))
            {
                query = includeDeleted ? dataSet : dataSet.Where(m => !EF.Property<bool>(m, nameof(BasePersistableEntity.Deleted)));
            }

            if (filter != null)
            {
                query = query.Where(filter);
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

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            try
            {
                dbContext.Dispose();
            }
            catch
            {
                // No throw on dispose
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
