using CQELight.Abstractions.DAL.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CQELight.TestFramework.Extensions.DAL
{
    /// <summary>
    /// Collection of extension methods upon <see cref="Abstractions.DAL.Interfaces.IDataReaderRepository"/>.
    /// </summary>
    public static class IDataReaderRepositoryExtensions
    {
        #region Public static methods

        /// <summary>
        /// Setting up repository mock to fake result of GetAsync method.
        /// </summary>
        /// <typeparam name="T">Type of repository to mock.</typeparam>
        /// <typeparam name="TEntity">Type of entity to mock.</typeparam>
        /// <param name="repositoryMock">Instance of repository mock.</param>
        /// <param name="expectedResult">Result that repository should returns.</param>
        public static void SetupGetAsyncReturns<T, TEntity>(
            this Mock<T> repositoryMock,
            IEnumerable<TEntity> expectedResult)
            where T : class, IDataReaderRepository
            where TEntity : class
        {
            repositoryMock.Setup(m => m.GetAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<Expression<Func<TEntity, object>>>(),
                  It.IsAny<bool>())).Returns(expectedResult.ToAsyncEnumerable());
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
            where T : class, IDataReaderRepository
            where TEntity : class
        {
            if (times == null)
            {
                times = Times.AtLeastOnce();
            }
            repository.Verify(m => m.GetAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<Expression<Func<TEntity, object>>>(),
                  It.IsAny<bool>()), times.Value);
        }

        #endregion

    }
}
