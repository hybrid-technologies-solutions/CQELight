using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using CQELight.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.DAL.EFCore.Adapters
{
    class EFCoreDataReaderAdapter : DisposableObject, IDataReaderAdapter
    {
        #region Members

        private readonly BaseDbContext dbContext;

        #endregion

        #region Ctor

        public EFCoreDataReaderAdapter(
            BaseDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        #endregion

        #region IDataReaderAdapter methods

        public IAsyncEnumerable<T> GetAsync<T>(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool includeDeleted = false) where T : class
            => GetCore(filter, orderBy, includeDeleted).ToAsyncEnumerable();

        public Task<T> GetByIdAsync<T>(object value) where T : class
            => dbContext.Set<T>().FindAsync(value);

        #endregion

        #region Protected methods

        protected virtual IQueryable<T> GetCore<T>(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool includeDeleted = false)
            where T : class
        {
            var dataSet = dbContext.Set<T>();
            IQueryable<T> query = dataSet;
            if (typeof(T).IsSubclassOf(typeof(BasePersistableEntity)))
            {
                query = includeDeleted ? dataSet : dataSet.Where(m => !(m as BasePersistableEntity).Deleted);
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
