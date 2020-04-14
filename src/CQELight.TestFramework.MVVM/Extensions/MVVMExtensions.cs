using CQELight.MVVM.Interfaces;
using Moq;
using System;

namespace CQELight.TestFramework.Extensions
{
    /// <summary>
    /// Collection of extensions for MVVM tests
    /// </summary>
    public static class MVVMExtensions
    {
        #region MVVM

        /// <summary>
        /// Retrieve a mock of IView that has already been configured by default.
        /// </summary>
        /// <returns>A pre-configured mock.</returns>
        public static Mock<IView> GetStandardWindowMock()
        {
            var mock = new Mock<IView>();

            //PerformOnUIThread should always be executed in test environment
            mock
                .Setup(m => m.PerformOnUIThread(It.IsAny<Action>()))
                .Callback((Action a) => a());

            return mock;
        }

        #endregion
    }
}
