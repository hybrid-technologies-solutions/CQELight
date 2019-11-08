using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CQELight.TestFramework
{
    public static class DALExtensions
    {
        #region Public static methods

        /// <summary>
        /// Fakes the id of an entity to allow to test it without to have to proceed to any repository mock callback.
        /// </summary>
        /// <param name="entity">Entity to fake the id.</param>
        /// <param name="desiredId">Desired id</param>
        public static void FakePersistenceId(this PersistableEntity entity, Guid desiredId)
            => entity.Id = desiredId;

        /// <summary>
        /// Create and easy setup upon a repository to fake the result of any Get on it.
        /// </summary>
        /// <typeparam name="T">Type of repository.</typeparam>
        /// <typeparam name="TEntity">Type of entity of the repository.</typeparam>
        /// <param name="repository">Mock instance of repository.</param>
        /// <param name="expectedResult">Results to returns when Get is called.</param>
        public static void SetupSimpleGetReturns<T, TEntity>(this Mock<T> repository,
            IEnumerable<TEntity> expectedResult)
            where T : class, IDataReaderRepository<TEntity>
            where TEntity : IPersistableEntity
        {
            repository.Setup(m => m.GetAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<Expression<Func<TEntity, object>>>(),
                  It.IsAny<bool>(), It.IsAny<Expression<Func<TEntity, object>>[]>())).Returns(expectedResult.ToAsyncEnumerable());
        }

        /// <summary>
        /// Perform an assertion that GetAsync has been called upon the specified mock of repository.
        /// </summary>
        /// <typeparam name="T">Type of repository to mock.</typeparam>
        /// <typeparam name="TEntity">Type of entity to mock</typeparam>
        /// <param name="repository">Repository mock instance.</param>
        /// <param name="times">Number of times to check if called.</param>
        public static void VerifyGetAsyncCalled<T, TEntity>(this Mock<T> repository,
            Times? times = null)
            where T : class, IDataReaderRepository<TEntity>
            where TEntity : IPersistableEntity
        {
            if (times == null)
            {
                times = Times.AtLeastOnce();
            }
            repository.Verify(m => m.GetAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<Expression<Func<TEntity, object>>>(),
                  It.IsAny<bool>(), It.IsAny<Expression<Func<TEntity, object>>[]>()), times.Value);
        }

        #endregion

    }
}
